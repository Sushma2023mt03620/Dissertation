from flask import Flask, request, jsonify
from flask_cors import CORS
import logging
from PredictiveMaintenanceModel import PredictiveMaintenanceModel

app = Flask(__name__)
CORS(app)

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Load model on startup
model = PredictiveMaintenanceModel()
try:
    model.load_model('models/predictive_maintenance_model.pkl')
    logger.info("Model loaded successfully")
except Exception as e:
    logger.error(f"Error loading model: {e}")

@app.route('/health', methods=['GET'])
def health_check():
    return jsonify({
        'status': 'healthy',
        'model_loaded': model.model is not None
    })

@app.route('/api/predict/maintenance', methods=['POST'])
def predict_maintenance():
    """Predict maintenance needs for a vehicle"""
    try:
        vehicle_data = request.json
        
        if not vehicle_data:
            return jsonify({'error': 'No data provided'}), 400
        
        # Make prediction
        result = model.predict(vehicle_data)
        
        logger.info(f"Prediction made for vehicle: {vehicle_data.get('vehicle_id', 'unknown')}")
        
        return jsonify(result), 200
    
    except Exception as e:
        logger.error(f"Prediction error: {e}")
        return jsonify({'error': str(e)}), 500

@app.route('/api/predict/batch', methods=['POST'])
def predict_batch():
    """Predict maintenance needs for multiple vehicles"""
    try:
        vehicles_data = request.json.get('vehicles', [])
        
        if not vehicles_data:
            return jsonify({'error': 'No vehicles data provided'}), 400
        
        results = []
        for vehicle in vehicles_data:
            prediction = model.predict(vehicle)
            prediction['vehicle_id'] = vehicle.get('vehicle_id')
            results.append(prediction)
        
        return jsonify({'predictions': results}), 200
    
    except Exception as e:
        logger.error(f"Batch prediction error: {e}")
        return jsonify({'error': str(e)}), 500

@app.route('/api/model/info', methods=['GET'])
def model_info():
    """Get model information"""
    return jsonify({
        'feature_names': model.feature_names,
        'model_type': 'Random Forest Classifier',
        'version': '1.0'
    })

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=False)