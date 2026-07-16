import React, { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { QRCodeSVG } from 'qrcode.react';
import apiClient from '../../api/apiClient';

interface LocationState {
  sessionId?: string;
  initialCode?: string;
  method?: 'pin' | 'qr' | 'face';
  sectionName?: string;
  sectionCode?: string;
  sectionId?: string;
  week?: number;
}

interface AttendanceRosterItem {
  studentName: string;
  studentNumber: string;
  attendancePercentage: number;
  attendedSessions: number;
  totalSessions: number;
  status: string; // Present, Absent, etc.
  method?: string; // face, qr, pin, system_default
  absenceWarning?: boolean;
  withdrawalPending?: boolean;
  isWithdrawn?: boolean;
  autoWithdrawnMessage?: string;
  enrollmentId?: string;
  finalizedAbsences: number;
}

export const InstructorAttendance: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const state = (location.state as LocationState) || {};

  const [code, setCode] = useState<string>(state.initialCode || '');

  const mapRosterResponse = (data: any): AttendanceRosterItem[] => {
    const pastWeeks = new Set<number>();
    (data.students || []).forEach((s: any) => {
      (s.attendanceRecords || []).forEach((r: any) => {
        if (state.week && r.week < state.week) {
          pastWeeks.add(r.week);
        }
      });
    });

    const total = state.week ? (pastWeeks.size + 1) : (data.totalSessions || 1);

    return (data.students || []).map((s: any) => {
      const currentRecord = s.attendanceRecords?.find((r: any) => r.sessionId === state.sessionId);
      const status = currentRecord ? currentRecord.status : 'Absent';
      
      const presentWeeks = new Set<number>();
      (s.attendanceRecords || []).forEach((r: any) => {
        if (r.status?.toLowerCase() === 'present' && state.week && r.week < state.week) {
          presentWeeks.add(r.week);
        }
      });
      if (status.toLowerCase() === 'present' && state.week) {
        presentWeeks.add(state.week);
      }
      const attended = presentWeeks.size;

      return {
        studentName: s.fullName,
        studentNumber: s.studentNumber,
        attendancePercentage: total === 0 ? 1 : attended / total,
        attendedSessions: attended,
        totalSessions: total,
        status: status,
        method: currentRecord?.method ?? undefined,
        absenceWarning: s.absenceWarning,
        withdrawalPending: s.withdrawalPending,
        isWithdrawn: s.isWithdrawn,
        autoWithdrawnMessage: s.autoWithdrawnMessage,
        enrollmentId: s.enrollmentId,
        finalizedAbsences: s.attendanceRecords?.filter((r: any) => r.status === 'absent' && state.week && r.week < state.week).length || 0
      };
    });
  };
  const [activeSession, setActiveSession] = useState<boolean>(!!state.sessionId);
  const [roster, setRoster] = useState<AttendanceRosterItem[]>([]);
  const [loadingRoster, setLoadingRoster] = useState(false);

  // Webcam and Face Check-in state
  const videoRef = React.useRef<HTMLVideoElement>(null);
  const [cameraActive, setCameraActive] = useState(false);
  const [detectionMessage, setDetectionMessage] = useState<string | null>("Scanning...");
  const [showDismissButton, setShowDismissButton] = useState(false);
  const [lastDetectedKey, setLastDetectedKey] = useState<string | null>(null);
  const [firstNotRegisteredTime, setFirstNotRegisteredTime] = useState<number | null>(null);
  const [paused, setPaused] = useState(false);
  const [promptMessage, setPromptMessage] = useState<string | null>(null);
  const [promptedEnrollments, setPromptedEnrollments] = useState<Record<string, number>>({});

  // Start webcam if face method is selected
  useEffect(() => {
    if (state.method !== 'face' || !activeSession) return;

    let stream: MediaStream | null = null;
    navigator.mediaDevices.getUserMedia({ video: { width: 640, height: 480 } })
      .then((s) => {
        stream = s;
        setCameraActive(true);
        if (videoRef.current) {
          videoRef.current.srcObject = s;
        }
      })
      .catch(() => {
        setDetectionMessage("Could not access webcam. Please ensure camera permissions are enabled.");
      });

    return () => {
      if (stream) {
        stream.getTracks().forEach(track => track.stop());
      }
    };
  }, [state.method, activeSession]);

  // Periodically send frames to face check-in endpoint
  useEffect(() => {
    if (state.method !== 'face' || !activeSession || !cameraActive || showDismissButton || paused) return;

    const canvas = document.createElement('canvas');
    canvas.width = 640;
    canvas.height = 480;
    const ctx = canvas.getContext('2d');

    const captureAndCheck = () => {
      if (showDismissButton || paused || !videoRef.current || !ctx) return;
      
      try {
        ctx.drawImage(videoRef.current, 0, 0, canvas.width, canvas.height);
        const dataUrl = canvas.toDataURL('image/jpeg', 0.8);
        
        apiClient.post(`/attendance/sessions/${state.sessionId}/face-checkin`, { image: dataUrl })
          .then((res) => {
            const matches = res.data;
            if (!matches || matches.length === 0) return;

            const primaryMatch = matches[0];
            if (primaryMatch.status === 'success') {
              setPromptMessage(`Attendance was registered for student: ${primaryMatch.name}`);
              setPaused(true);
              setFirstNotRegisteredTime(null);
              setLastDetectedKey(null);
              // Instantly refresh roster
              if (state.sectionId) {
                apiClient.get(`/sections/${state.sectionId}/attendance`)
                  .then(r => setRoster(mapRosterResponse(r.data)));
              }
            } 
            else if (primaryMatch.status === 'already_checked_in') {
              setPromptMessage(`${primaryMatch.name} already took attendance.`);
              setPaused(true);
              setFirstNotRegisteredTime(null);
              setLastDetectedKey(null);
            }
            else if (primaryMatch.status === 'not_registered') {
              const studentKey = primaryMatch.studentKey;
              
              if (lastDetectedKey === studentKey) {
                const elapsed = Date.now() - (firstNotRegisteredTime || Date.now());
                if (elapsed >= 5000) {
                  setPromptMessage("Student is not registered. Try different student");
                  setPaused(true);
                  setFirstNotRegisteredTime(null);
                  setLastDetectedKey(null);
                }
              } else {
                setLastDetectedKey(studentKey);
                setFirstNotRegisteredTime(Date.now());
              }
            } else if (primaryMatch.status === 'no_face') {
              // Do not reset firstNotRegisteredTime and lastDetectedKey to prevent transient drops from resetting the 5s timer.
            }
          })
          .catch(() => {
          });
      } catch {
      }
    };

    const interval = setInterval(captureAndCheck, 1500);
    return () => clearInterval(interval);
  }, [state.method, activeSession, cameraActive, showDismissButton, lastDetectedKey, firstNotRegisteredTime, paused]);

  const handleNextStudent = () => {
    setPaused(false);
    setPromptMessage(null);
    setShowDismissButton(false);
    setFirstNotRegisteredTime(null);
    setLastDetectedKey(null);
    setDetectionMessage("Scanning...");
  };

  const handleDismissNotRegistered = () => {
    setShowDismissButton(false);
    setFirstNotRegisteredTime(null);
    setLastDetectedKey(null);
    setDetectionMessage("Scanning...");
  };

  const handleWithdrawalDecision = (enrollmentId: string, approve: boolean) => {
    apiClient.post(`/sections/${state.sectionId}/enrollments/${enrollmentId}/withdrawal-decision`, { approve })
      .then(() => {
        alert(approve ? "Student successfully withdrawn." : "Withdrawal request dismissed.");
        if (state.sectionId) {
          loadRoster(state.sectionId);
        }
      })
      .catch((err) => {
        alert(err.response?.data?.message || 'Failed to record withdrawal decision.');
      });
  };

  // Poll for QR rotation
  useEffect(() => {
    if (!state.sessionId || state.method !== 'qr' || !activeSession) return;

    const fetchCode = () => {
      apiClient.get<{ currentCode: string }>(`/attendance/sessions/${state.sessionId}/code`)
        .then((res) => {
          setCode(res.data.currentCode);
        })
        .catch(() => {
        });
    };

    fetchCode(); // fetch immediately
    const interval = setInterval(fetchCode, 15000); // 15 seconds

    return () => clearInterval(interval);
  }, [state.sessionId, state.method, activeSession]);

  // Handle interactive prompts for real-time warnings
  useEffect(() => {
    if (!activeSession || roster.length === 0) return;

    const processPrompts = async () => {
      const updatedPrompted = { ...promptedEnrollments };
      let changed = false;

      for (const student of roster) {
        if (!student.enrollmentId) continue;
        const abs = student.finalizedAbsences;

        // Skip if already prompted for this absence level
        if (updatedPrompted[student.enrollmentId] === abs) continue;

        if (abs === 2) {
          alert(`⚠️ Warning: Student ${student.studentName} has been absent 2 times.`);
          updatedPrompted[student.enrollmentId] = abs;
          changed = true;
        } 
        else if (abs === 3 && student.withdrawalPending) {
          updatedPrompted[student.enrollmentId] = abs;
          changed = true;
          setPromptedEnrollments(updatedPrompted);
          const approve = window.confirm(`🚨 Student ${student.studentName} has missed 3 classes. Do you want to approve their withdrawal? Click OK to withdraw, or Cancel to let it go.`);
          try {
            await apiClient.post(`/sections/${state.sectionId}/enrollments/${student.enrollmentId}/withdrawal-decision`, { approve });
          } catch {
          }
        } 
        else if (abs >= 4 && student.isWithdrawn) {
          alert(`🚫 Student ${student.studentName} was automatically withdrawn after 4 absences.`);
          updatedPrompted[student.enrollmentId] = abs;
          changed = true;
        }
      }

      if (changed) {
        setPromptedEnrollments(updatedPrompted);
      }
    };

    processPrompts();
  }, [roster, activeSession, promptedEnrollments, state.sectionId]);

  // Load roster
  const loadRoster = (sectionId: string) => {
    setLoadingRoster(true);
    apiClient.get(`/sections/${sectionId}/attendance`)
      .then((res) => {
        setRoster(mapRosterResponse(res.data));
      })
      .catch(() => {
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
      apiClient.get(`/sections/${state.sectionId}/attendance`)
        .then((res) => {
          setRoster(mapRosterResponse(res.data));
        })
        .catch(() => {
        });
    }, 5000);

    return () => clearInterval(interval);
  }, [state.sectionId, activeSession]);

  const handleCloseSession = () => {
    if (!state.sessionId) return;
    apiClient.put(`/attendance/sessions/${state.sessionId}/close`)
      .then(() => {
        if (state.sectionId) {
          apiClient.get(`/sections/${state.sectionId}/attendance`)
            .then(async (res) => {
              const students = mapRosterResponse(res.data);
              const rawStudents = res.data.students || [];
              
              for (const student of students) {
                const rawStudent = rawStudents.find((rs: any) => rs.enrollmentId === student.enrollmentId);
                if (rawStudent) {
                  const absences = rawStudent.attendanceRecords?.filter((r: any) => r.status === 'absent' && (!state.week || r.week <= state.week)).length || 0;
                  
                  if (absences === 2) {
                    alert(`⚠️ Warning: Student ${student.studentName} has missed 2 classes.`);
                  }
                  else if (absences === 3 && rawStudent.withdrawalPending) {
                    const approve = window.confirm(`🚨 Student ${student.studentName} has missed 3 classes. Do you want to approve their withdrawal? Click OK to withdraw, or Cancel to let it go.`);
                    await apiClient.post(`/sections/${state.sectionId}/enrollments/${student.enrollmentId}/withdrawal-decision`, { approve });
                  }
                  else if (absences === 4 && rawStudent.isWithdrawn) {
                    alert(`🚫 Student ${student.studentName} was automatically withdrawn after 4 absences.`);
                  }
                }
              }
              
              alert('Attendance session successfully closed.');
              setActiveSession(false);
              navigate('/instructor/sections');
            })
            .catch(() => {
              setActiveSession(false);
              navigate('/instructor/sections');
            });
        } else {
          setActiveSession(false);
          alert('Attendance session successfully closed.');
          navigate('/instructor/sections');
        }
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
              ) : state.method === 'qr' ? (
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px' }}>
                  {code ? (
                    <>
                      <div style={{ padding: '16px', background: '#fff', borderRadius: '8px', display: 'inline-block' }}>
                        <QRCodeSVG
                          value={code}
                          size={200}
                          level="H"
                        />
                      </div>
                      <p style={{
                        fontSize: '2rem',
                        fontWeight: 'bold',
                        letterSpacing: '4px',
                        color: 'var(--accent)',
                        fontFamily: 'monospace',
                        margin: '12px 0',
                      }}>
                        {code}
                      </p>
                    </>
                  ) : (
                    <div style={{ width: '232px', height: '232px', display: 'flex', justifyContent: 'center', alignItems: 'center', color: 'var(--text-muted)' }}>
                      Generating QR...
                    </div>
                  )}
                  <div style={{ fontSize: '0.85rem', color: 'var(--accent)' }}>
                    🔄 Token rotates automatically every 15 seconds.
                  </div>
                </div>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '16px', width: '100%' }}>
                  <div style={{ width: '100%', padding: '12px 16px', background: 'rgba(99,102,241,0.12)', border: '1px solid rgba(99,102,241,0.4)', borderRadius: '8px', fontSize: '0.85rem', color: '#a5b4fc', textAlign: 'center' }}>
                    📱 Students must open the mobile app and tap <strong>Face Scan</strong> to check in — or use the webcam below for on-site recognition.
                  </div>
                  <div style={{ position: 'relative', width: '320px', height: '240px', borderRadius: '12px', overflow: 'hidden', background: '#000', border: '2px solid var(--accent)' }}>
                    <video ref={videoRef} autoPlay playsInline muted style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
                  </div>
                  {!paused ? (
                    <>
                      <div style={{ fontWeight: 'bold', color: 'var(--accent)' }}>
                        {detectionMessage}
                      </div>
                      {showDismissButton && (
                        <button
                          className="glass-btn primary"
                          style={{ padding: '6px 16px', fontSize: '0.85rem' }}
                          onClick={handleDismissNotRegistered}
                        >
                          Next student
                        </button>
                      )}
                    </>
                  ) : (
                    <div style={{
                      margin: '8px 0',
                      padding: '16px',
                      borderRadius: '8px',
                      background: 'rgba(30,41,59,0.95)',
                      border: '1px solid var(--accent)',
                      textAlign: 'center',
                      width: '100%',
                      maxWidth: '300px'
                    }}>
                      <p style={{ margin: '0 0 12px 0', fontSize: '0.95rem', fontWeight: 'bold', color: '#fff' }}>
                        {promptMessage}
                      </p>
                      <button
                        className="glass-btn primary"
                        style={{ padding: '6px 20px', fontSize: '0.85rem', width: '100%', justifyContent: 'center' }}
                        onClick={handleNextStudent}
                      >
                        Next student
                      </button>
                    </div>
                  )}
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
                    <th style={{ padding: '12px' }}>Method</th>
                    <th style={{ padding: '12px' }}>Current Session</th>
                  </tr>
                </thead>
                <tbody>
                  {roster.map((student, index) => (
                    <tr key={index} style={{ borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                      <td style={{ padding: '12px' }}>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
                          <span style={{ fontWeight: 'bold' }}>{student.studentName}</span>
                          {student.absenceWarning && (
                            <span style={{ padding: '2px 6px', background: 'rgba(245, 158, 11, 0.15)', color: '#fbbf24', border: '1px solid #f59e0b', borderRadius: '4px', fontSize: '0.7rem' }}>
                              ⚠️ Warning: 2+ Absences
                            </span>
                          )}
                        </div>
                        <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{student.studentNumber}</div>
                        {student.withdrawalPending && (
                          <div style={{ marginTop: '8px', background: 'rgba(239, 68, 68, 0.1)', border: '1px solid var(--error)', borderRadius: '6px', padding: '8px 12px', display: 'inline-flex', flexDirection: 'column', gap: '8px' }}>
                            <div style={{ fontSize: '0.75rem', color: 'var(--error)', fontWeight: 'bold' }}>
                              🚨 Pending Withdrawal Decision (3 Absences)
                            </div>
                            <div style={{ display: 'flex', gap: '8px' }}>
                              <button 
                                className="glass-btn" 
                                style={{ padding: '2px 8px', fontSize: '0.75rem', borderColor: 'var(--error)', color: 'var(--error)', background: 'rgba(239, 68, 68, 0.1)' }}
                                onClick={() => handleWithdrawalDecision(student.enrollmentId!, true)}
                              >
                                Approve Withdrawal
                              </button>
                              <button 
                                className="glass-btn" 
                                style={{ padding: '2px 8px', fontSize: '0.75rem' }}
                                onClick={() => handleWithdrawalDecision(student.enrollmentId!, false)}
                              >
                                Let it go
                              </button>
                            </div>
                          </div>
                        )}
                        {student.isWithdrawn && student.autoWithdrawnMessage && (
                          <div style={{ marginTop: '4px', fontSize: '0.8rem', color: 'var(--error)', fontWeight: 500 }}>
                            🚫 {student.autoWithdrawnMessage}
                          </div>
                        )}
                      </td>
                      <td style={{ padding: '12px' }}>{(student.attendancePercentage * 100).toFixed(1)}%</td>
                      <td style={{ padding: '12px' }}>{student.attendedSessions} / {student.totalSessions}</td>
                      <td style={{ padding: '12px' }}>
                        {student.method ? (
                          <span style={{ fontSize: '0.8rem', padding: '2px 8px', borderRadius: '4px', background: student.method === 'face' ? 'rgba(99,102,241,0.15)' : student.method === 'qr' ? 'rgba(16,185,129,0.15)' : 'rgba(245,158,11,0.15)', color: student.method === 'face' ? '#a5b4fc' : student.method === 'qr' ? 'var(--success)' : '#fbbf24' }}>
                            {student.method === 'face' ? '🤖 Face' : student.method === 'qr' ? '📷 QR' : student.method === 'pin' ? '🔢 PIN' : student.method}
                          </span>
                        ) : <span style={{ color: 'var(--text-muted)', fontSize: '0.8rem' }}>—</span>}
                      </td>
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
