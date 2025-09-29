import React, { useState, useEffect } from 'react';
import { LineChart, Line, BarChart, Bar, XAxis, YAxis, CartesianGrid, 
         Tooltip, Legend, ResponsiveContainer } from 'recharts';
import * as signalR from '@microsoft/signalr';
import './Dashboard.css';

const Dashboard = () => {
  const [oeeData, setOeeData] = useState([]);
  const [realTimeMetrics, setRealTimeMetrics] = useState({
    activeJobs: 0,
    completedToday: 0,
    defectRate: 0,
    avgOEE: 0
  });
  const [alerts, setAlerts] = useState([]);
  const [connection, setConnection] = useState(null);

  useEffect(() => {
    // Fetch initial data
    fetchDashboardData();

    // Setup SignalR connection for real-time updates
    setupSignalRConnection();

    // Refresh data every 30 seconds
    const interval = setInterval(fetchDashboardData, 30000);

    return () => {
      clearInterval(interval);
      if (connection) {
        connection.stop();
      }
    };
  }, []);

  const setupSignalRConnection = async () => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://api.automotive-platform.com/hubs/production')
      .withAutomaticReconnect()
      .build();

    newConnection.on('OEEUpdate', (data) => {
      console.log('OEE Update received:', data);
      updateOEEData(data);
    });

    newConnection.on('AlertReceived', (alert) => {
      console.log('Alert received:', alert);
      setAlerts(prev => [alert, ...prev].slice(0, 10));
    });

    newConnection.on('MetricsUpdate', (metrics) => {
      setRealTimeMetrics(metrics);
    });

    try {
      await newConnection.start();
      console.log('SignalR Connected');
      setConnection(newConnection);
    } catch (err) {
      console.error('SignalR Connection Error:', err);
    }
  };

  const fetchDashboardData = async () => {
    try {
      const response = await fetch('https://api.automotive-platform.com/api/dashboard/summary');
      const data = await response.json();
      
      setRealTimeMetrics(data.metrics);
      setOeeData(data.oeeHistory);
      setAlerts(data.recentAlerts);
    } catch (error) {
      console.error('Error fetching dashboard data:', error);
    }
  };

  const updateOEEData = (newData) => {
    setOeeData(prev => {
      const updated = [...prev];
      const index = updated.findIndex(d => d.lineId === newData.lineId);
      if (index >= 0) {
        updated[index] = newData;
      } else {
        updated.push(newData);
      }
      return updated;
    });
  };

  return (
    <div className="dashboard">
      <h1>Manufacturing Dashboard</h1>

      {/* KPI Cards */}
      <div className="kpi-grid">
        <KPICard
          title="Active Jobs"
          value={realTimeMetrics.activeJobs}
          trend="+5%"
          icon="📊"
        />
        <KPICard
          title="Completed Today"
          value={realTimeMetrics.completedToday}
          trend="+12%"
          icon="✅"
        />
        <KPICard
          title="Defect Rate"
          value={`${realTimeMetrics.defectRate.toFixed(2)}%`}
          trend="-2%"
          icon="⚠️"
          trendPositive={false}
        />
        <KPICard
          title="Average OEE"
          value={`${(realTimeMetrics.avgOEE * 100).toFixed(1)}%`}
          trend="+3%"
          icon="📈"
        />
      </div>

      {/* Charts Section */}
      <div className="charts-grid">
        <div className="chart-card">
          <h2>OEE Trends by Production Line</h2>
          <ResponsiveContainer width="100%" height={300}>
            <LineChart data={oeeData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="timestamp" />
              <YAxis domain={[0, 1]} tickFormatter={(value) => `${(value * 100).toFixed(0)}%`} />
              <Tooltip formatter={(value) => `${(value * 100).toFixed(1)}%`} />
              <Legend />
              <Line type="monotone" dataKey="oee" stroke="#8884d8" name="OEE" />
              <Line type="monotone" dataKey="availability" stroke="#82ca9d" name="Availability" />
              <Line type="monotone" dataKey="performance" stroke="#ffc658" name="Performance" />
              <Line type="monotone" dataKey="quality" stroke="#ff7c7c" name="Quality" />
            </LineChart>
          </ResponsiveContainer>
        </div>

        <div className="chart-card">
          <h2>Production by Line</h2>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={oeeData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="lineId" />
              <YAxis />
              <Tooltip />
              <Legend />
              <Bar dataKey="producedQuantity" fill="#8884d8" name="Produced" />
              <Bar dataKey="plannedQuantity" fill="#82ca9d" name="Planned" />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>

      {/* Alerts Section */}
      <div className="alerts-section">
        <h2>Recent Alerts</h2>
        <div className="alerts-list">
          {alerts.length === 0 ? (
            <p className="no-alerts">No recent alerts</p>
          ) : (
            alerts.map((alert, index) => (
              <AlertCard key={index} alert={alert} />
            ))
          )}
        </div>
      </div>
    </div>
  );
};

const KPICard = ({ title, value, trend, icon, trendPositive = true }) => (
  <div className="kpi-card">
    <div className="kpi-icon">{icon}</div>
    <div className="kpi-content">
      <h3>{title}</h3>
      <div className="kpi-value">{value}</div>
      <div className={`kpi-trend ${trendPositive ? 'positive' : 'negative'}`}>
        {trend}
      </div>
    </div>
  </div>
);

const AlertCard = ({ alert }) => (
  <div className={`alert-card severity-${alert.severity.toLowerCase()}`}>
    <div className="alert-header">
      <span className="alert-type">{alert.type}</span>
      <span className="alert-time">{new Date(alert.timestamp).toLocaleTimeString()}</span>
    </div>
    <div className="alert-message">{alert.message}</div>
    <div className="alert-source">Source: {alert.source}</div>
  </div>
);

export default Dashboard;