import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import apiClient from '../../api/apiClient';

interface Section {
  id: number;
  courseCode: string;
  courseName: string;
  scheduleJson: string;
  capacity: number;
}

export const InstructorSections: React.FC = () => {
  const [sections, setSections] = useState<Section[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Modal / Session creation state
  const [selectedSection, setSelectedSection] = useState<Section | null>(null);
  const [method, setMethod] = useState<'pin' | 'qr'>('pin');
  const [minutes, setMinutes] = useState<number>(15);
  const [lat, setLat] = useState<number>(30.0444);
  const [lng, setLng] = useState<number>(31.2357);
  const [radius, setRadius] = useState<number>(50);
  const [submitting, setSubmitting] = useState(false);

  const navigate = useNavigate();

  useEffect(() => {
    apiClient.get<Section[]>('/instructor/sections')
      .then((res) => {
        setSections(res.data);
      })
      .catch((err) => {
        console.error(err);
        setError('Failed to load instructor sections.');
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  const handleStartSession = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSection) return;

    setSubmitting(true);
    const payload = {
      sectionId: selectedSection.id,
      method,
      minutes,
      lat,
      lng,
      radiusMeters: radius
    };

    apiClient.post('/attendance/sessions', payload)
      .then((res) => {
        // Redirect to attendance monitor page
        navigate('/instructor/attendance', {
          state: {
            sessionId: res.data.sessionId,
            initialCode: res.data.currentCode,
            method,
            sectionName: selectedSection.courseName,
            sectionCode: selectedSection.courseCode,
          }
        });
      })
      .catch((err) => {
        alert(err.response?.data?.message || 'Failed to start session.');
      })
      .finally(() => {
        setSubmitting(false);
        setSelectedSection(null);
      });
  };

  const formatSchedule = (json: string) => {
    try {
      const items = JSON.parse(json);
      if (Array.isArray(items)) {
        const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
        return items.map((it: any) => {
          const dayVal = it.day ?? it.Day;
          const day = typeof dayVal === 'string'
            ? dayVal
            : (days[it.dayOfWeek ?? it.DayOfWeek] || 'Day');
          const start = it.startTime ?? it.StartTime ?? '';
          const end = it.endTime ?? it.EndTime ?? '';
          const room = it.room ?? it.Room;
          return `${day} ${start}–${end}${room ? ` (${room})` : ''}`;
        }).join(', ');
      }
    } catch {
      return 'TBD';
    }
    return 'TBD';
  };

  if (loading) {
    return <div className="brand-subtitle">Loading assigned sections...</div>;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px' }}>
        <h1 style={{ marginBottom: '8px' }}>My Sections</h1>
        <p style={{ color: 'var(--text-muted)' }}>Overview of your assigned sections, weekly schedules, and active attendance controls.</p>
      </div>

      {error ? (
        <div style={{ color: 'var(--error)' }}>{error}</div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '24px' }}>
          {sections.map((section) => (
            <div key={section.id} className="glass-panel" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div>
                <span style={{ fontSize: '0.8rem', color: 'var(--accent)', fontWeight: 'bold' }}>SECTION ID: {section.id}</span>
                <h3 style={{ margin: '4px 0 8px 0' }}>{section.courseCode}</h3>
                <p style={{ color: 'var(--text-main)', fontWeight: 500 }}>{section.courseName}</p>
                <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)', marginTop: '8px' }}>
                  📅 Timetable: {formatSchedule(section.scheduleJson)}
                </div>
                <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
                  👥 Capacity: {section.capacity} students
                </div>
              </div>

              <div style={{ display: 'flex', gap: '12px', marginTop: 'auto' }}>
                <button
                  className="glass-btn primary"
                  style={{ flex: 1, justifyContent: 'center' }}
                  onClick={() => setSelectedSection(section)}
                >
                  ⏱️ Start Attendance
                </button>
                <button
                  className="glass-btn"
                  style={{ flex: 1, justifyContent: 'center' }}
                  onClick={() => navigate('/instructor/grading', { state: { sectionId: section.id, sectionName: section.courseName, sectionCode: section.courseCode } })}
                >
                  ✍️ Grade Roster
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {selectedSection && (
        <div style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: 'rgba(0,0,0,0.8)',
          zIndex: 1000,
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          padding: '20px'
        }}>
          <div className="glass-panel" style={{ width: '100%', maxWidth: '500px', padding: '32px', background: '#1e293b' }}>
            <h2 style={{ marginBottom: '16px' }}>Start Session: {selectedSection.courseCode}</h2>
            <form onSubmit={handleStartSession} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div className="form-group">
                <label className="form-label">Method</label>
                <select
                  className="form-input"
                  style={{ background: 'rgba(15,23,42,0.9)' }}
                  value={method}
                  onChange={(e) => setMethod(e.target.value as 'pin' | 'qr')}
                >
                  <option value="pin">Numeric PIN (Static)</option>
                  <option value="qr">Rotating Token (QR Code)</option>
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Duration (Minutes)</label>
                <input
                  type="number"
                  className="form-input"
                  min={1}
                  value={minutes}
                  onChange={(e) => setMinutes(Number(e.target.value))}
                />
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group">
                  <label className="form-label">Latitude</label>
                  <input
                    type="number"
                    step="0.000001"
                    className="form-input"
                    value={lat}
                    onChange={(e) => setLat(Number(e.target.value))}
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">Longitude</label>
                  <input
                    type="number"
                    step="0.000001"
                    className="form-input"
                    value={lng}
                    onChange={(e) => setLng(Number(e.target.value))}
                  />
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">Geofence Radius (Meters)</label>
                <input
                  type="number"
                  className="form-input"
                  min={5}
                  value={radius}
                  onChange={(e) => setRadius(Number(e.target.value))}
                />
              </div>

              <div style={{ display: 'flex', gap: '16px', marginTop: '16px' }}>
                <button
                  type="button"
                  className="glass-btn"
                  style={{ flex: 1, justifyContent: 'center' }}
                  onClick={() => setSelectedSection(null)}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="glass-btn primary"
                  style={{ flex: 1, justifyContent: 'center' }}
                  disabled={submitting}
                >
                  {submitting ? 'Creating...' : 'Launch Session'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
