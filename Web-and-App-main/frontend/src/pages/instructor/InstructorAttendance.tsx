import React, { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { QRCodeSVG } from 'qrcode.react';
import apiClient from '../../api/apiClient';

interface LocationState {
  sessionId?: number;
  initialCode?: string;
  method?: 'pin' | 'qr';
  sectionName?: string;
  sectionCode?: string;
  sectionId?: number;
}

interface AttendanceRosterItem {
  studentName: string;
  studentNumber: string;
  attendancePercentage: number;
  attendedSessions: number;
  totalSessions: number;
  status: string; // Present, Absent, etc.
}

export const InstructorAttendance: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const state = (location.state as LocationState) || {};

  const [code, setCode] = useState<string>(state.initialCode || '');
  const [activeSession, setActiveSession] = useState<boolean>(!!state.sessionId);
  const [roster, setRoster] = useState<AttendanceRosterItem[]>([]);
  const [loadingRoster, setLoadingRoster] = useState(false);

  // Poll for QR rotation
  useEffect(() => {
    if (!state.sessionId || state.method !== 'qr' || !activeSession) return;

    const fetchCode = () => {
      apiClient.get<{ currentCode: string }>(`/attendance/sessions/${state.sessionId}/code`)
        .then((res) => {
          setCode(res.data.currentCode);
        })
        .catch((err) => {
          console.error('Failed to rotate session code', err);
        });
    };

    fetchCode(); // fetch immediately
    const interval = setInterval(fetchCode, 15000); // 15 seconds

    return () => clearInterval(interval);
  }, [state.sessionId, state.method, activeSession]);

  // Load roster
  const loadRoster = (sectionId: number) => {
    setLoadingRoster(true);
    apiClient.get<AttendanceRosterItem[]>(`/sections/${sectionId}/attendance`)
      .then((res) => {
        setRoster(res.data);
      })
      .catch((err) => {
        console.error('Failed to load attendance roster', err);
      })
      .finally(() => {
        setLoadingRoster(false);
      });
  };

  // If a session is active, load the roster initially and poll every 5 seconds
  useEffect(() => {
    if (!state.sectionId || !activeSession) return;
    loadRoster(state.sectionId);

    const interval = setInterval(() => {
      apiClient.get<AttendanceRosterItem[]>(`/sections/${state.sectionId}/attendance`)
        .then((res) => {
          setRoster(res.data);
        })
        .catch((err) => {
          console.error('Polling error', err);
        });
    }, 5000);

    return () => clearInterval(interval);
  }, [state.sectionId, activeSession]);

  const handleCloseSession = () => {
    if (!state.sessionId) return;
    apiClient.put(`/attendance/sessions/${state.sessionId}/close`)
      .then(() => {
        setActiveSession(false);
        alert('Attendance session successfully closed.');
        navigate('/instructor/sections');
      })
      .catch((err) => {
        alert(err.response?.data?.message || 'Failed to close session.');
      });
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px' }}>
        <h1 style={{ marginBottom: '8px' }}>Attendance Control Board</h1>
        <p style={{ color: 'var(--text-muted)' }}>Monitor real-time check-ins and session details.</p>
      </div>

      {activeSession ? (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '24px' }}>
            <div className="glass-panel" style={{ padding: '32px', textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: '16px' }}>
              <h3 style={{ color: 'var(--accent)' }}>Active Session Code</h3>
              <p style={{ color: 'var(--text-muted)' }}>
                Course: <strong>{state.sectionCode} - {state.sectionName}</strong>
              </p>

              {state.method === 'pin' ? (
                <div style={{
                  textAlign: 'center',
                  padding: '24px',
                }}>
                  <p style={{ color: '#aaa', marginBottom: '8px', fontSize: '14px' }}>
                    Session PIN Code
                  </p>
                  <p style={{
                    fontSize: '56px',
                    fontWeight: 'bold',
                    letterSpacing: '12px',
                    color: '#f59e0b',    // amber/yellow — visible on dark background
                    fontFamily: 'monospace',
                    margin: '12px 0',
                  }}>
                    {code}
                  </p>
                  <p style={{ color: '#aaa', fontSize: '12px', marginTop: '8px' }}>
                    PIN is valid for the entire session duration
                  </p>
                </div>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px' }}>
                  {code ? (
                    <div style={{ padding: '16px', background: '#fff', borderRadius: '8px', display: 'inline-block' }}>
                      <QRCodeSVG
                        value={code}
                        size={200}
                        level="H"
                      />
                    </div>
                  ) : (
                    <div style={{ width: '232px', height: '232px', display: 'flex', justifyContent: 'center', alignItems: 'center', color: 'var(--text-muted)' }}>
                      Generating QR...
                    </div>
                  )}
                  <div style={{ fontSize: '0.85rem', color: 'var(--accent)' }}>
                    🔄 Token rotates automatically every 15 seconds.
                  </div>
                </div>
              )}

              <button
                className="glass-btn"
                style={{ borderColor: 'var(--error)', color: 'var(--error)', background: 'rgba(239, 68, 68, 0.05)', marginTop: '24px', width: '100%', justifyContent: 'center' }}
                onClick={handleCloseSession}
              >
                🛑 Stop Session & Close Attendance
              </button>
            </div>

            <div className="glass-panel" style={{ padding: '24px' }}>
              <h3 style={{ marginBottom: '16px' }}>Quick Instructions</h3>
              <ul style={{ color: 'var(--text-muted)', display: 'flex', flexDirection: 'column', gap: '12px', paddingLeft: '20px' }}>
                <li>Students must check in via the mobile application with <strong>X-Client-Platform: mobile</strong>.</li>
                <li>Location validation checks if students are within the configured geofence radius.</li>
                <li>PIN remains static, while QR rotates automatically for anti-proxy security.</li>
              </ul>
            </div>
          </div>

          <div className="glass-panel" style={{ padding: '24px' }}>
            <h3 style={{ marginBottom: '16px' }}>Real-time Attendance Status</h3>
            {loadingRoster && roster.length === 0 ? (
              <p style={{ color: 'var(--text-muted)' }}>Loading roster...</p>
            ) : roster.length === 0 ? (
              <p style={{ color: 'var(--text-muted)' }}>No student check-in records yet for this session.</p>
            ) : (
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)', fontSize: '0.85rem' }}>
                    <th style={{ padding: '12px' }}>Student</th>
                    <th style={{ padding: '12px' }}>Attendance %</th>
                    <th style={{ padding: '12px' }}>Attended / Total</th>
                    <th style={{ padding: '12px' }}>Current Session</th>
                  </tr>
                </thead>
                <tbody>
                  {roster.map((student, index) => (
                    <tr key={index} style={{ borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                      <td style={{ padding: '12px' }}>
                        <div style={{ fontWeight: 'bold' }}>{student.studentName}</div>
                        <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{student.studentNumber}</div>
                      </td>
                      <td style={{ padding: '12px' }}>{(student.attendancePercentage * 100).toFixed(1)}%</td>
                      <td style={{ padding: '12px' }}>{student.attendedSessions} / {student.totalSessions}</td>
                      <td style={{ padding: '12px' }}>
                        <span style={{
                          padding: '4px 8px',
                          borderRadius: '4px',
                          fontSize: '0.8rem',
                          fontWeight: 'bold',
                          background: student.status.toLowerCase() === 'present' ? 'rgba(16, 185, 129, 0.15)' : 'rgba(239, 68, 68, 0.15)',
                          color: student.status.toLowerCase() === 'present' ? 'var(--success)' : 'var(--error)',
                          border: student.status.toLowerCase() === 'present' ? '1px solid var(--success)' : '1px solid var(--error)'
                        }}>
                          {student.status}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      ) : (
        <div className="glass-panel" style={{ padding: '40px', textAlign: 'center', color: 'var(--text-muted)' }}>
          No active session is running. Go to <strong style={{ cursor: 'pointer', color: 'var(--accent)' }} onClick={() => navigate('/instructor/sections')}>My Sections</strong> to start one.
        </div>
      )}
    </div>
  );
};
