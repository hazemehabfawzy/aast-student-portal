import React, { useEffect, useState } from 'react';
import apiClient from '../../api/apiClient';

interface Section {
  sectionId: string;
  courseCode: string;
  courseName: string;
  instructorName: string;
  scheduleJson: string;
  capacity: number;
  enrolledCount: number;
  isEnrolled: boolean;
  enrollmentId?: string;
}

interface AlternativeSection {
  sectionId: string;
  courseCode: string;
  schedule: string;
  seatsLeft: number;
}

interface ConflictResponse {
  conflict: boolean;
  conflictingWith: string;
  alternatives: AlternativeSection[];
}

export const StudentRegister: React.FC = () => {
  const [sections, setSections] = useState<Section[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionMessage, setActionMessage] = useState<{ text: string; isError: boolean } | null>(null);
  const [conflictData, setConflictData] = useState<ConflictResponse | null>(null);

  const fetchSections = () => {
    setLoading(true);
    // Semester 1 is the seeded semester
    apiClient.get<Section[]>('/sections?semesterId=1')
      .then((res) => {
        setSections(res.data);
        setError(null);
      })
      .catch((err) => {
        console.error(err);
        setError('Registration is closed or not available for your department/year level.');
      })
      .finally(() => {
        setLoading(false);
      });
  };

  useEffect(() => {
    fetchSections();
  }, []);

  const handleEnroll = (sectionId: string) => {
    setActionMessage(null);
    setConflictData(null);
    apiClient.post('/enrollments', { sectionId })
      .then(() => {
        setActionMessage({ text: 'Successfully enrolled!', isError: false });
        fetchSections();
      })
      .catch((err) => {
        const errorData = err.response?.data;
        if (err.response && err.response.status === 409 && errorData?.conflict) {
          setConflictData(errorData);
        } else {
          setActionMessage({
            text: errorData?.message || 'Failed to register for section.',
            isError: true,
          });
        }
      });
  };

  const handleUnenroll = (enrollmentId: string) => {
    if (!window.confirm('Are you sure you want to drop this course?')) return;
    setActionMessage(null);
    setConflictData(null);
    apiClient.delete(`/enrollments/${enrollmentId}`)
      .then(() => {
        setActionMessage({ text: 'Successfully dropped course!', isError: false });
        fetchSections();
      })
      .catch((err) => {
        setActionMessage({
          text: err.response?.data?.message || 'Failed to drop course.',
          isError: true,
        });
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

  if (loading && sections.length === 0) {
    return <div className="brand-subtitle">Loading course options...</div>;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px' }}>
        <h1 style={{ marginBottom: '8px' }}>Course Registration</h1>
        <p style={{ color: 'var(--text-muted)' }}>Browse available sections and manage your class enrollments.</p>
      </div>

      {actionMessage && (
        <div style={{
          padding: '16px',
          background: actionMessage.isError ? 'rgba(239, 68, 68, 0.15)' : 'rgba(16, 185, 129, 0.15)',
          border: actionMessage.isError ? '1px solid var(--error)' : '1px solid var(--success)',
          color: actionMessage.isError ? 'var(--error)' : 'var(--success)',
          borderRadius: '8px',
          fontWeight: 500,
        }}>
          {actionMessage.text}
        </div>
      )}

      {conflictData && (
        <div style={{
          padding: '24px',
          background: 'rgba(239, 68, 68, 0.1)',
          border: '1px solid var(--error)',
          borderRadius: '8px',
          display: 'flex',
          flexDirection: 'column',
          gap: '16px',
        }}>
          <div style={{ fontWeight: 'bold', color: 'var(--error)', fontSize: '1.1rem' }}>
            ⚠️ Schedule Conflict Detected
          </div>
          <p style={{ color: 'var(--text-main)', margin: 0 }}>
            This section conflicts with: {conflictData.conflictingWith}
          </p>
          
          {conflictData.alternatives.length > 0 && (
            <div>
              <div style={{ fontWeight: 600, marginBottom: '12px', color: 'var(--accent)' }}>
                Recommended No-Conflict Alternatives:
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                {conflictData.alternatives.map((alt) => (
                  <div key={alt.sectionId} style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    padding: '12px 16px',
                    background: 'rgba(255,255,255,0.03)',
                    borderRadius: '6px',
                    border: '1px solid var(--border-color)',
                  }}>
                    <div>
                      <span style={{ fontWeight: 'bold' }}>{alt.courseCode}</span>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                        Timetable: {alt.schedule} | Seats Left: {alt.seatsLeft}
                      </div>
                    </div>
                    <button 
                      className="glass-btn primary"
                      style={{ padding: '6px 12px', fontSize: '0.85rem' }}
                      onClick={() => handleEnroll(alt.sectionId)}
                    >
                      Quick Enroll
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {error ? (
        <div className="glass-panel" style={{ padding: '24px', color: 'var(--text-muted)', textAlign: 'center' }}>
          {error}
        </div>
      ) : (
        <div className="glass-panel" style={{ padding: '24px', overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)', fontSize: '0.9rem' }}>
                <th style={{ padding: '16px 12px' }}>Course</th>
                <th style={{ padding: '16px 12px' }}>Instructor</th>
                <th style={{ padding: '16px 12px' }}>Schedule</th>
                <th style={{ padding: '16px 12px' }}>Available Seats</th>
                <th style={{ padding: '16px 12px', textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {sections.map((sec) => (
                <tr key={sec.sectionId} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                  <td style={{ padding: '16px 12px' }}>
                    <div style={{ fontWeight: 'bold' }}>{sec.courseCode}</div>
                    <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>{sec.courseName}</div>
                  </td>
                  <td style={{ padding: '16px 12px' }}>{sec.instructorName}</td>
                  <td style={{ padding: '16px 12px', fontSize: '0.9rem' }}>{formatSchedule(sec.scheduleJson)}</td>
                  <td style={{ padding: '16px 12px' }}>
                    {sec.enrolledCount} / {sec.capacity}
                  </td>
                  <td style={{ padding: '16px 12px', textAlign: 'right' }}>
                    {sec.isEnrolled ? (
                      <button
                        className="glass-btn"
                        style={{ borderColor: 'var(--error)', color: 'var(--error)', background: 'rgba(239, 68, 68, 0.05)' }}
                        onClick={() => sec.enrollmentId && handleUnenroll(sec.enrollmentId)}
                      >
                        Drop Course
                      </button>
                    ) : (
                      <button
                        className="glass-btn primary"
                        disabled={sec.enrolledCount >= sec.capacity}
                        onClick={() => handleEnroll(sec.sectionId)}
                      >
                        Register
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
