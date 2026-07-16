import React, { useEffect, useMemo, useState } from 'react';
import apiClient from '../../api/apiClient';

interface Section {
  id: string;
  courseCode: string;
  courseName: string;
  credits: number;
  instructorName: string;
  scheduleJson: string;
  capacity: number;
  enrolledCount: number;
  semesterNumber: number;
  semester: string;
  prerequisiteCode?: string | null;
  prerequisiteMet: boolean;
  alreadyEnrolled: boolean;
  enrollmentId?: string | null;
}

interface AvailableResponse {
  sections: Section[];
  passedCourses: string[];
  currentSemesterNumber: number;
  registrationOpen: boolean;
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
  const [currentSemesterNumber, setCurrentSemesterNumber] = useState(1);
  const [registrationOpen, setRegistrationOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionMessage, setActionMessage] = useState<{ text: string; isError: boolean } | null>(null);
  const [conflictData, setConflictData] = useState<ConflictResponse | null>(null);
  const [semesterFilter, setSemesterFilter] = useState<number | 'all' | 'current'>('current');
  const [searchQuery, setSearchQuery] = useState('');

  const fetchSections = () => {
    setLoading(true);
    apiClient.get<AvailableResponse>('/sections/available')
      .then((res) => {
        setSections(res.data.sections);
        setCurrentSemesterNumber(res.data.currentSemesterNumber);
        setRegistrationOpen(res.data.registrationOpen);
        setError(null);
      })
      .catch(() => {
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
        return items.map((it: Record<string, unknown>) => {
          const dayVal = it.day ?? it.Day;
          const day = typeof dayVal === 'string'
            ? dayVal
            : (days[(it.dayOfWeek ?? it.DayOfWeek) as number] || 'Day');
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

  const semesterNumbers = useMemo(
    () => [...new Set(sections.map((s) => s.semesterNumber))].sort((a, b) => a - b),
    [sections],
  );

  const filteredSections = useMemo(() => {
    return sections.filter((sec) => {
      const matchesSemester =
        semesterFilter === 'all'
          ? true
          : semesterFilter === 'current'
            ? sec.semesterNumber === currentSemesterNumber
            : sec.semesterNumber === semesterFilter;

      const q = searchQuery.trim().toLowerCase();
      const matchesSearch =
        q === ''
        || sec.courseCode.toLowerCase().includes(q)
        || sec.courseName.toLowerCase().includes(q);

      return matchesSemester && matchesSearch;
    });
  }, [sections, semesterFilter, searchQuery, currentSemesterNumber]);

  const groupedSections = useMemo(() => {
    const groups: Record<number, Section[]> = {};
    filteredSections.forEach((sec) => {
      if (!groups[sec.semesterNumber]) groups[sec.semesterNumber] = [];
      groups[sec.semesterNumber].push(sec);
    });
    return Object.entries(groups)
      .sort(([a], [b]) => Number(a) - Number(b))
      .map(([num, secs]) => ({ semesterNumber: Number(num), sections: secs }));
  }, [filteredSections]);

  if (loading && sections.length === 0) {
    return <div className="brand-subtitle">Loading course options...</div>;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px' }}>
        <h1 style={{ marginBottom: '8px' }}>Course Registration</h1>
        <p style={{ color: 'var(--text-muted)' }}>
          Browse available sections and manage your class enrollments.
          {!registrationOpen && sections.length > 0 && (
            <span style={{ color: 'var(--error)', marginLeft: '8px' }}>
              (Registration period is currently closed)
            </span>
          )}
        </p>
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

      {!error && sections.length > 0 && (
        <div className="glass-panel" style={{ padding: '20px', display: 'flex', flexWrap: 'wrap', gap: '16px', alignItems: 'center' }}>
          <div style={{ flex: '1 1 220px' }}>
            <input
              type="text"
              placeholder="Search by course name or code..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              style={{
                width: '100%',
                padding: '10px 14px',
                borderRadius: '8px',
                border: '1px solid var(--border-color)',
                background: 'rgba(255,255,255,0.03)',
                color: 'var(--text-main)',
              }}
            />
          </div>
          <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
            <button
              className={`glass-btn ${semesterFilter === 'current' ? 'primary' : ''}`}
              onClick={() => setSemesterFilter('current')}
            >
              Current (Sem {currentSemesterNumber})
            </button>
            <button
              className={`glass-btn ${semesterFilter === 'all' ? 'primary' : ''}`}
              onClick={() => setSemesterFilter('all')}
            >
              All Semesters
            </button>
            {semesterNumbers.map((num) => (
              <button
                key={num}
                className={`glass-btn ${semesterFilter === num ? 'primary' : ''}`}
                onClick={() => setSemesterFilter(num)}
              >
                Sem {num}
              </button>
            ))}
          </div>
        </div>
      )}

      {error ? (
        <div className="glass-panel" style={{ padding: '24px', color: 'var(--text-muted)', textAlign: 'center' }}>
          {error}
        </div>
      ) : groupedSections.length === 0 ? (
        <div className="glass-panel" style={{ padding: '24px', color: 'var(--text-muted)', textAlign: 'center' }}>
          No courses match your filters.
        </div>
      ) : (
        groupedSections.map(({ semesterNumber, sections: semesterSections }) => (
          <div key={semesterNumber} className="glass-panel" style={{ padding: '24px' }}>
            <h2 style={{ marginBottom: '16px', color: 'var(--accent)' }}>
              Semester {semesterNumber}
            </h2>
            <div style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
              gap: '16px',
            }}>
              {semesterSections.map((section) => {
                const isLocked = !section.prerequisiteMet;
                const isFull = section.enrolledCount >= section.capacity;

                return (
                  <div
                    key={section.id}
                    style={{
                      opacity: isLocked ? 0.6 : 1,
                      border: `1px solid ${
                        section.alreadyEnrolled ? '#4CAF50'
                          : isLocked ? '#EF4444'
                            : '#2A4A6A'}`,
                      borderRadius: '12px',
                      padding: '16px',
                      background: 'rgba(255,255,255,0.02)',
                    }}
                  >
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '8px' }}>
                      <div>
                        <div style={{ fontWeight: 'bold', fontSize: '1.05rem' }}>{section.courseCode}</div>
                        <div style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>{section.courseName}</div>
                      </div>
                      {section.alreadyEnrolled && (
                        <span style={{
                          background: 'rgba(76, 175, 80, 0.15)',
                          color: '#4CAF50',
                          padding: '4px 8px',
                          borderRadius: '6px',
                          fontSize: '12px',
                          fontWeight: 600,
                          whiteSpace: 'nowrap',
                        }}>
                          ✓ Enrolled
                        </span>
                      )}
                    </div>

                    <div style={{ marginTop: '12px', fontSize: '0.85rem', color: 'var(--text-muted)', display: 'flex', flexDirection: 'column', gap: '4px' }}>
                      <span>Credits: {section.credits}</span>
                      <span>Instructor: {section.instructorName}</span>
                      <span>Schedule: {formatSchedule(section.scheduleJson)}</span>
                      <span>Seats: {section.enrolledCount} / {section.capacity}</span>
                    </div>

                    {isLocked && section.prerequisiteCode && (
                      <div style={{ color: '#EF4444', fontSize: '12px', marginTop: '8px' }}>
                        🔒 Requires: {section.prerequisiteCode}
                      </div>
                    )}

                    <div style={{ marginTop: '16px' }}>
                      {section.alreadyEnrolled && section.enrollmentId ? (
                        <button
                          className="glass-btn"
                          style={{ borderColor: 'var(--error)', color: 'var(--error)', background: 'rgba(239, 68, 68, 0.05)', width: '100%' }}
                          onClick={() => handleUnenroll(section.enrollmentId!)}
                        >
                          Drop Course
                        </button>
                      ) : (
                        <button
                          className="glass-btn primary"
                          style={{ width: '100%' }}
                          disabled={isLocked || isFull || !registrationOpen}
                          onClick={() => handleEnroll(section.id)}
                        >
                          {isLocked ? 'Locked' : isFull ? 'Full' : 'Register'}
                        </button>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        ))
      )}
    </div>
  );
};
