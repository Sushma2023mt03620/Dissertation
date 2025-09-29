import pandas as pd
import numpy as np
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler
import joblib
import json

class PredictiveMaintenanceModel:
    def __init__(self):
        self.model = None
        self.scaler = StandardScaler()
        self.feature_names = [
            'engine_temperature',
            'vibration_level',
            'oil_pressure',
            'mileage',
            'engine_rpm',
            'fuel_consumption',
            'brake_wear',
            'tire_pressure',
            'battery_voltage',
            'coolant_level',
            'days_since_last_maintenance',
            'harsh_braking_count',
            'harsh_acceleration_count',
            'average_speed',
            'idle_time_percentage'
        ]
    
    def prepare_training_data(self, data_path):
        """Load and prepare training data"""
        df = pd.read_csv(data_path)
        
        # Feature engineering
        df['maintenance_urgency'] = df.apply(
            lambda row: self._calculate_urgency(row), axis=1
        )
        
        # Create binary target: needs maintenance in next 14 days
        df['needs_maintenance'] = (df['days_until_failure'] <= 14).astype(int)
        
        X = df[self.feature_names]
        y = df['needs_maintenance']
        
        return train_test_split(X, y, test_size=0.2, random_state=42)
    
    def train(self, data_path):
        """Train the predictive maintenance model"""
        X_train, X_test, y_train, y_test = self.prepare_training_data(data_path)
        
        # Scale features
        X_train_scaled = self.scaler.fit_transform(X_train)
        X_test_scaled = self.scaler.transform(X_test)
        
        # Train Random Forest model
        self.model = RandomForestClassifier(
            n_estimators=200,
            max_depth=15,
            min_samples_split=5,
            min_samples_leaf=2,
            random_state=42,
            class_weight='balanced'
        )
        
        self.model.fit(X_train_scaled, y_train)
        
        # Evaluate
        train_score = self.model.score(X_train_scaled, y_train)
        test_score = self.model.score(X_test_scaled, y_test)
        
        print(f"Training Accuracy: {train_score:.4f}")
        print(f"Testing Accuracy: {test_score:.4f}")
        
        # Feature importance
        feature_importance = pd.DataFrame({
            'feature': self.feature_names,
            'importance': self.model.feature_importances_
        }).sort_values('importance', ascending=False)
        
        print("\nTop 5 Important Features:")
        print(feature_importance.head())
        
        return test_score
    
    def predict(self, vehicle_data):
        """Predict maintenance needs for a vehicle"""
        if self.model is None:
            raise ValueError("Model not trained. Call train() first.")
        
        # Prepare features
        features = np.array([[
            vehicle_data.get(feature, 0) for feature in self.feature_names
        ]])
        
        # Scale and predict
        features_scaled = self.scaler.transform(features)
        prediction = self.model.predict(features_scaled)[0]
        probability = self.model.predict_proba(features_scaled)[0][1]
        
        # Calculate days until maintenance
        days_estimate = self._estimate_days_until_maintenance(
            probability, vehicle_data
        )
        
        return {
            'needs_maintenance': bool(prediction),
            'probability': float(probability),
            'estimated_days': int(days_estimate),
            'urgency': self._get_urgency_level(probability),
            'recommended_actions': self._get_recommendations(
                vehicle_data, probability
            )
        }
    
    def _calculate_urgency(self, row):
        """Calculate maintenance urgency score"""
        urgency = 0
        
        if row['engine_temperature'] > 100:
            urgency += 30
        if row['vibration_level'] > 8:
            urgency += 25
        if row['oil_pressure'] < 30:
            urgency += 35
        if row['days_since_last_maintenance'] > 180:
            urgency += 20
        
        return min(urgency, 100)
    
    def _estimate_days_until_maintenance(self, probability, vehicle_data):
        """Estimate days until maintenance is needed"""
        base_days = 14
        
        if probability > 0.8:
            return max(1, base_days * (1 - probability))
        elif probability > 0.5:
            return base_days
        else:
            return base_days + (base_days * (1 - probability))
    
    def _get_urgency_level(self, probability):
        """Get urgency level based on probability"""
        if probability > 0.8:
            return "CRITICAL"
        elif probability > 0.6:
            return "HIGH"
        elif probability > 0.4:
            return "MEDIUM"
        else:
            return "LOW"
    
    def _get_recommendations(self, vehicle_data, probability):
        """Generate maintenance recommendations"""
        recommendations = []
        
        if vehicle_data.get('engine_temperature', 0) > 95:
            recommendations.append("Check cooling system")
        
        if vehicle_data.get('vibration_level', 0) > 7:
            recommendations.append("Inspect engine mounts and suspension")
        
        if vehicle_data.get('oil_pressure', 100) < 35:
            recommendations.append("Check oil levels and pressure sensor")
        
        if vehicle_data.get('brake_wear', 0) > 80:
            recommendations.append("Replace brake pads")
        
        if vehicle_data.get('days_since_last_maintenance', 0) > 150:
            recommendations.append("Schedule routine maintenance")
        
        if probability > 0.7 and not recommendations:
            recommendations.append("Comprehensive vehicle inspection recommended")
        
        return recommendations
    
    def save_model(self, model_path='predictive_maintenance_model.pkl'):
        """Save trained model to disk"""
        joblib.dump({
            'model': self.model,
            'scaler': self.scaler,
            'feature_names': self.feature_names
        }, model_path)
        print(f"Model saved to {model_path}")
    
    def load_model(self, model_path='predictive_maintenance_model.pkl'):
        """Load trained model from disk"""
        data = joblib.load(model_path)
        self.model = data['model']
        self.scaler = data['scaler']
        self.feature_names = data['feature_names']
        print(f"Model loaded from {model_path}")

# Example usage
if __name__ == "__main__":
    model = PredictiveMaintenanceModel()
    
    # Train the model
    accuracy = model.train('training_data/vehicle_maintenance_history.csv')
    
    # Save model
    model.save_model()
    
    # Example prediction
    test_vehicle = {
        'engine_temperature': 92,
        'vibration_level': 7.5,
        'oil_pressure': 45,
        'mileage': 85000,
        'engine_rpm': 2800,
        'fuel_consumption': 8.5,
        'brake_wear': 65,
        'tire_pressure': 32,
        'battery_voltage': 12.6,
        'coolant_level': 85,
        'days_since_last_maintenance': 120,
        'harsh_braking_count': 15,
        'harsh_acceleration_count': 10,
        'average_speed': 65,
        'idle_time_percentage': 15
    }
    
    result = model.predict(test_vehicle)
    print("\nPrediction Result:")
    print(json.dumps(result, indent=2))