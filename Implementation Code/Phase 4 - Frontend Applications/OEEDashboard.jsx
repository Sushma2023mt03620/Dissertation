import React, { useState, useEffect } from 'react';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, Legend } from 'recharts';

const OEEDashboard = () => {
  const [oeeData, setOeeData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [selectedLine, setSelectedLine] = useState('line-001');

  useEffect(() => {
    fetchOEEData();
    const interval = setInterval(fetchOEEData, 30000); // Refresh every 30s
    return () => clearInterval(interval);
  }, [selectedLine]);

  const fetchOEEData = async () => {
    try {
      const response = await fetch(
        `https://api.automotive-platform.com/api/production/oee/${selectedLine}`
      );
      const data = await response.json();
      setOeeData(data);
      setLoading(false);
    } catch (error) {
      console.error('Error fetching OEE data:', error);
      setLoading(false);
    }
  };

  if (loading) return <div className="loading">Loading...</div>;

  return (
    <div className="oee-dashboard">
      <h1>Production Line OEE Monitoring</h1>
      
      <div className="line-selector">
        <select value={selectedLine} onChange={(e) => setSelectedLine(e.target.value)}>
          <option value="line-001">Production Line 1</option>
          <option value="line-002">Production Line 2</option>
          <option value="line-003">Production Line 3</option>
        </select>
      </div>

      <div className="metrics-grid">
        <MetricCard
          title="Overall OEE"
          value={`${(oeeData.oee * 100).toFixed(1)}%`}
          status={oeeData.oee > 0.85 ? 'good' : oeeData.oee > 0.75 ? 'warning' : 'critical'}
        />
        <MetricCard
          title="Availability"
          value={`${(oeeData.availability * 100).toFixed(1)}%`}
        />
        <MetricCard
          title="Performance"
          value={`${(oeeData.performance * 100).toFixed(1)}%`}
        />
        <MetricCard
          title="Quality"
          value={`${(oeeData.quality * 100).toFixed(1)}%`}
        />
      </div>

      <div className="chart-container">
        <h2>OEE Trend (Last 7 Days)</h2>
        {/* Add historical chart here */}
      </div>
    </div>
  );
};

const MetricCard = ({ title, value, status = 'normal' }) => (
  <div className={`metric-card ${status}`}>
    <h3>{title}</h3>
    <div className="value">{value}</div>
  </div>
);

export default OEEDashboard;
