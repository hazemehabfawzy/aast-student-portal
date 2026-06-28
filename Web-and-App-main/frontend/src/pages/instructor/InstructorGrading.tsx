import React, { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import apiClient from '../../api/apiClient';

interface Section {
  id: string;
  courseCode: string;
  courseName: string;
}

interface StudentGrade {
  enrollmentId: string;
  studentNumber: string;
  studentName: string;
  week7Score?: number;
  week12Score?: number;
  prefinalScore?: number;
  finalScore?: number;
  totalScore?: number;
  letterGrade?: string;
  published: boolean;
}

export const InstructorGrading: React.FC = () => {
  const location = useLocation();
  const state = (location.state as { sectionId?: string; sectionCode?: string; sectionName?: string }) || {};

  const [sections, setSections] = useState<Section[]>([]);
  const [selectedSectionId, setSelectedSectionId] = useState<string>(state.sectionId || '');
  const [grades, setGrades] = useState<StudentGrade[]>([]);
  const [loading, setLoading] = useState(false);
  const [savingId, setSavingId] = useState<string | null>(null);

  // Editable scores locally
  const [editScores, setEditScores] = useState<Record<string, {
    week7Score: string;
    week12Score: string;
    prefinalScore: string;
    finalScore: string;
  }>>({});

  useEffect(() => {
    apiClient.get<Section[]>('/instructor/sections')
      .then((res) => {
        setSections(res.data);
        if (!selectedSectionId && res.data.length > 0) {
          setSelectedSectionId(res.data[0].id);
        }
      })
      .catch((err) => console.error('Failed to load instructor sections', err));
  }, []);

  useEffect(() => {
    if (!selectedSectionId) return;

    setLoading(true);
    apiClient.get<StudentGrade[]>(`/sections/${selectedSectionId}/results`)
      .then((res) => {
        setGrades(res.data);
        const initialEdit: typeof editScores = {};
        res.data.forEach((g) => {
          initialEdit[g.enrollmentId] = {
            week7Score: g.week7Score?.toString() ?? '',
            week12Score: g.week12Score?.toString() ?? '',
            prefinalScore: g.prefinalScore?.toString() ?? '',
            finalScore: g.finalScore?.toString() ?? '',
          };
        });
        setEditScores(initialEdit);
      })
      .catch((err) => console.error('Failed to load grades', err))
      .finally(() => setLoading(false));
  }, [selectedSectionId]);

  const handleScoreChange = (enrollmentId: string, field: string, value: string) => {
    if (value === '') {
      setEditScores(prev => ({
        ...prev,
        [enrollmentId]: {
          ...prev[enrollmentId],
          [field]: ''
        }
      }));
      return;
    }

    let max = 100;
    if (field === 'week7Score') max = 30;
    else if (field === 'week12Score') max = 20;
    else if (field === 'prefinalScore') max = 10;
    else if (field === 'finalScore') max = 40;

    const num = parseFloat(value);
    let finalVal = value;
    if (!isNaN(num)) {
      const clamped = Math.min(max, Math.max(0, num));
      finalVal = clamped.toString();
    }

    setEditScores(prev => ({
      ...prev,
      [enrollmentId]: {
        ...prev[enrollmentId],
        [field]: finalVal
      }
    }));
  };

  const handleSave = (enrollmentId: string) => {
    const scores = editScores[enrollmentId];
    if (!scores) return;

    setSavingId(enrollmentId);
    const payload = {
      week7Score: scores.week7Score === '' ? null : parseFloat(scores.week7Score),
      week12Score: scores.week12Score === '' ? null : parseFloat(scores.week12Score),
      prefinalScore: scores.prefinalScore === '' ? null : parseFloat(scores.prefinalScore),
      finalScore: scores.finalScore === '' ? null : parseFloat(scores.finalScore),
    };

    apiClient.put(`/results/${enrollmentId}`, payload)
      .then(() => {
        alert('Scores updated successfully and grade recalculated.');
        if (selectedSectionId) {
          apiClient.get<StudentGrade[]>(`/sections/${selectedSectionId}/results`)
            .then((res) => {
              setGrades(res.data);
            });
        }
      })
      .catch((err) => {
        alert(err.response?.data?.message || 'Failed to update scores.');
      })
      .finally(() => {
        setSavingId(null);
      });
  };

  const handlePublish = (enrollmentId: string) => {
    if (!window.confirm('Are you sure you want to publish this result? The student will be notified.')) return;

    apiClient.post(`/results/${enrollmentId}/publish`)
      .then(() => {
        alert('Result published successfully.');
        setGrades(prev => prev.map(g => g.enrollmentId === enrollmentId ? { ...g, published: true } : g));
      })
      .catch((err) => {
        alert(err.response?.data?.message || 'Failed to publish result.');
      });
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <h1 style={{ marginBottom: '8px' }}>Grading Portal</h1>
          <p style={{ color: 'var(--text-muted)' }}>Input final and continuous assessment grades for your students.</p>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <label style={{ fontSize: '0.9rem', color: 'var(--text-muted)' }}>Select Section:</label>
          <select
            className="form-input"
            style={{ width: '220px', background: 'rgba(15,23,42,0.9)' }}
            value={selectedSectionId}
            onChange={(e) => setSelectedSectionId(e.target.value)}
          >
            <option value="">-- Choose Section --</option>
            {sections.map(s => (
              <option key={s.id} value={s.id}>{s.courseCode} - {s.courseName}</option>
            ))}
          </select>
        </div>
      </div>

      {selectedSectionId === '' ? (
        <div className="glass-panel" style={{ padding: '40px', textAlign: 'center', color: 'var(--text-muted)' }}>
          Please select a section from the dropdown list to view student lists.
        </div>
      ) : loading ? (
        <div className="brand-subtitle">Loading student grade roster...</div>
      ) : (
        <div className="glass-panel" style={{ padding: '24px', overflowX: 'auto' }}>
          {grades.length === 0 ? (
            <p style={{ color: 'var(--text-muted)', textAlign: 'center' }}>No students enrolled in this section.</p>
          ) : (
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)', fontSize: '0.85rem' }}>
                  <th style={{ padding: '12px' }}>Student</th>
                  <th style={{ padding: '12px', width: '100px' }}>Week 7 / 30</th>
                  <th style={{ padding: '12px', width: '100px' }}>Week 12 / 20</th>
                  <th style={{ padding: '12px', width: '100px' }}>Prefinal / 10</th>
                  <th style={{ padding: '12px', width: '100px' }}>Final / 40</th>
                  <th style={{ padding: '12px', width: '95px' }}>Total / 100</th>
                  <th style={{ padding: '12px', width: '80px' }}>Grade</th>
                  <th style={{ padding: '12px', width: '100px' }}>Status</th>
                  <th style={{ padding: '12px', textAlign: 'right' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {grades.map((grade) => {
                  const edit = editScores[grade.enrollmentId] || { week7Score: '', week12Score: '', prefinalScore: '', finalScore: '' };
                  return (
                    <tr key={grade.enrollmentId} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                      <td style={{ padding: '12px' }}>
                        <div style={{ fontWeight: 'bold' }}>
                          {grade.studentName}
                          {grade.published && (
                            <span style={{ fontSize: '11px', color: '#F59E0B', marginLeft: '6px' }}>
                              Published
                            </span>
                          )}
                        </div>
                        <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{grade.studentNumber}</div>
                      </td>
                      <td style={{ padding: '8px' }}>
                        <input
                          type="number"
                          step="0.5"
                          min={0}
                          max={30}
                          placeholder="0–30"
                          className="form-input"
                          style={{ padding: '6px 8px', fontSize: '0.9rem', width: '80px' }}
                          value={edit.week7Score}
                          onChange={(e) => handleScoreChange(grade.enrollmentId, 'week7Score', e.target.value)}
                        />
                      </td>
                      <td style={{ padding: '8px' }}>
                        <input
                          type="number"
                          step="0.5"
                          min={0}
                          max={20}
                          placeholder="0–20"
                          className="form-input"
                          style={{ padding: '6px 8px', fontSize: '0.9rem', width: '80px' }}
                          value={edit.week12Score}
                          onChange={(e) => handleScoreChange(grade.enrollmentId, 'week12Score', e.target.value)}
                        />
                      </td>
                      <td style={{ padding: '8px' }}>
                        <input
                          type="number"
                          step="0.5"
                          min={0}
                          max={10}
                          placeholder="0–10"
                          className="form-input"
                          style={{ padding: '6px 8px', fontSize: '0.9rem', width: '80px' }}
                          value={edit.prefinalScore}
                          onChange={(e) => handleScoreChange(grade.enrollmentId, 'prefinalScore', e.target.value)}
                        />
                      </td>
                      <td style={{ padding: '8px' }}>
                        <input
                          type="number"
                          step="0.5"
                          min={0}
                          max={40}
                          placeholder="0–40"
                          className="form-input"
                          style={{ padding: '6px 8px', fontSize: '0.9rem', width: '80px' }}
                          value={edit.finalScore}
                          onChange={(e) => handleScoreChange(grade.enrollmentId, 'finalScore', e.target.value)}
                        />
                        {edit.finalScore !== '' && parseFloat(edit.finalScore) < 12 && (
                          <div style={{ color: '#ef4444', fontSize: '11px', marginTop: '4px', whiteSpace: 'nowrap' }}>
                            ⚠ Min 12 required
                          </div>
                        )}
                      </td>
                      <td style={{ padding: '12px', fontWeight: 'bold', color: 'var(--accent)' }}>
                        {grade.totalScore !== undefined ? grade.totalScore : '-'}
                      </td>
                      <td style={{ padding: '12px' }}>
                        {grade.letterGrade ? (
                          <span style={{ fontWeight: 'bold', color: 'var(--success)' }}>{grade.letterGrade}</span>
                        ) : (
                          <span style={{ color: 'var(--text-muted)' }}>-</span>
                        )}
                      </td>
                      <td style={{ padding: '12px' }}>
                        {grade.published ? (
                          <span style={{ color: 'var(--success)', fontSize: '0.85rem' }}>📢 Published</span>
                        ) : (
                          <span style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>Draft</span>
                        )}
                      </td>
                      <td style={{ padding: '12px', textAlign: 'right' }}>
                        <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                          <button
                            className="glass-btn primary"
                            style={{ padding: '6px 12px', fontSize: '0.8rem' }}
                            disabled={savingId === grade.enrollmentId}
                            onClick={() => handleSave(grade.enrollmentId)}
                          >
                            {savingId === grade.enrollmentId ? 'Saving...' : 'Save'}
                          </button>
                          <button
                            className="glass-btn"
                            style={{ padding: '6px 12px', fontSize: '0.8rem', borderColor: 'var(--accent)', color: 'var(--accent)' }}
                            disabled={grade.published}
                            onClick={() => handlePublish(grade.enrollmentId)}
                          >
                            Publish
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  );
};
