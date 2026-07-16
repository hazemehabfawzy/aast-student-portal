import React, { useEffect, useState } from 'react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ReferenceLine,
  ResponsiveContainer,
} from 'recharts';
import apiClient from '../../api/apiClient';

interface ScoreRange {
  low: number;
  high: number;
}

interface Prediction {
  predictedFinal: number;
  predictedTotal: number;
  finalRange: ScoreRange;
  totalRange: ScoreRange;
  atRisk: boolean;
  riskLevel: 'LOW' | 'MEDIUM' | 'HIGH';
}

interface StudentResult {
  courseCode: string;
  courseName: string;
  creditHours: number;
  semesterName: string;
  week7Score?: number;
  week12Score?: number;
  prefinalScore?: number;
  finalScore?: number;
  totalScore?: number;
  letterGrade?: string;
  published: boolean;
  prediction?: Prediction;
}

interface GpaTrendPoint {
  semester: string;
  gpa: number;
}

interface ResultsResponse {
  cumulativeGpa: number;
  academicStanding: string;
  results: StudentResult[];
}

export const StudentResults: React.FC = () => {
  const [results, setResults] = useState<StudentResult[]>([]);
  const [gpaTrend, setGpaTrend] = useState<GpaTrendPoint[]>([]);
  const [cumulativeGpa, setCumulativeGpa] = useState<number>(0);
  const [academicStanding, setAcademicStanding] = useState<string>('Pass');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    apiClient.get<ResultsResponse>('/students/me/results')
      .then((res) => {
        setResults(res.data.results || []);
        setCumulativeGpa(res.data.cumulativeGpa || 0);
        setAcademicStanding(res.data.academicStanding || 'Pass');
      })
      .catch(() => {
        setError('Failed to retrieve academic results.');
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  useEffect(() => {
    apiClient.get<GpaTrendPoint[]>('/students/me/gpa-trend')
      .then((r) => setGpaTrend(r.data))
      .catch(() => {});
  }, []);

  if (loading) {
    return <div className="brand-subtitle">Loading academic results...</div>;
  }

  if (error) {
    return <div style={{ color: 'var(--error)' }}>{error}</div>;
  }

  // Group results by semesterName
  const groupedResults = results.reduce((acc, r) => {
    const sem = r.semesterName || 'Other';
    if (!acc[sem]) {
      acc[sem] = [];
    }
    acc[sem].push(r);
    return acc;
  }, {} as Record<string, StudentResult[]>);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Title Panel */}
      <div className="glass-panel" style={{ padding: '32px' }}>
        <h1 style={{ marginBottom: '8px' }}>Academic Results</h1>
        <p style={{ color: 'var(--text-muted)' }}>Official grading sheets and continuous assessment scores.</p>
      </div>

      {/* GPA & Standing Summary Cards */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: '24px' }}>
        <div className="glass-panel" style={{ padding: '24px', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', textAlign: 'center' }}>
          <span style={{ fontSize: '0.9rem', color: 'var(--text-muted)', marginBottom: '8px', textTransform: 'uppercase', letterSpacing: '1px' }}>Cumulative GPA</span>
          <span style={{ fontSize: '2.5rem', fontWeight: 'bold', color: 'var(--success)', background: 'linear-gradient(to right, #10b981, #34d399)', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent' }}>
            {cumulativeGpa.toFixed(2)}
          </span>
        </div>
        <div className="glass-panel" style={{ padding: '24px', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', textAlign: 'center' }}>
          <span style={{ fontSize: '0.9rem', color: 'var(--text-muted)', marginBottom: '8px', textTransform: 'uppercase', letterSpacing: '1px' }}>Academic Standing</span>
          <span style={{ fontSize: '1.8rem', fontWeight: 'bold', color: 'var(--accent)' }}>
            {academicStanding}
          </span>
        </div>
      </div>

      {gpaTrend.length > 0 && (
        <div
          style={{
            background: '#1A2F45',
            borderRadius: '12px',
            padding: '20px',
            marginBottom: '24px',
            border: '1px solid #2A4A6A',
          }}
        >
          <h3 style={{ color: '#fff', marginBottom: '16px', fontSize: '16px' }}>
            📈 GPA Trend
          </h3>
          <ResponsiveContainer width="100%" height={200}>
            <LineChart data={gpaTrend}>
              <CartesianGrid strokeDasharray="3 3" stroke="#2A4A6A" />
              <XAxis dataKey="semester" stroke="#8AAAC8" tick={{ fontSize: 12 }} />
              <YAxis domain={[0, 4]} stroke="#8AAAC8" tick={{ fontSize: 12 }} />
              <Tooltip
                contentStyle={{
                  background: '#0D1B2A',
                  border: '1px solid #4A90E2',
                  borderRadius: '8px',
                  color: '#fff',
                }}
              />
              <ReferenceLine
                y={2.0}
                stroke="#EF4444"
                strokeDasharray="4 4"
                label={{ value: 'Min 2.0', fill: '#EF4444', fontSize: 11 }}
              />
              <ReferenceLine
                y={3.5}
                stroke="#4CAF50"
                strokeDasharray="4 4"
                label={{ value: "Dean's List", fill: '#4CAF50', fontSize: 11 }}
              />
              <Line
                type="monotone"
                dataKey="gpa"
                stroke="#4A90E2"
                strokeWidth={3}
                dot={{ fill: '#4A90E2', r: 5 }}
                activeDot={{ r: 7 }}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* Results Tables Grouped by Semester */}
      {results.length === 0 ? (
        <div className="glass-panel" style={{ padding: '48px', textAlign: 'center' }}>
          <p style={{ color: 'var(--text-muted)' }}>No results published or enrolled sections found.</p>
        </div>
      ) : (
        Object.entries(groupedResults).map(([semesterName, semesterResults]) => (
          <div key={semesterName} className="glass-panel" style={{ padding: '24px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <h2 style={{ fontSize: '1.25rem', color: 'var(--accent)', borderBottom: '1px solid rgba(255,255,255,0.08)', paddingBottom: '12px', margin: 0 }}>
              {semesterName}
            </h2>
            <div style={{ overflowX: 'auto' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)', fontSize: '0.9rem' }}>
                    <th style={{ padding: '16px 12px' }}>Course</th>
                    <th style={{ padding: '16px 12px' }}>Credits</th>
                    <th style={{ padding: '16px 12px' }}>7th / 30</th>
                    <th style={{ padding: '16px 12px' }}>12th / 20</th>
                    <th style={{ padding: '16px 12px' }}>C.Work / 10</th>
                    <th style={{ padding: '16px 12px' }}>Final / 40</th>
                    <th style={{ padding: '16px 12px' }}>Total / 100</th>
                    <th style={{ padding: '16px 12px' }}>Grade</th>
                  </tr>
                </thead>
                <tbody>
                  {semesterResults.map((r, i) => (
                    <React.Fragment key={i}>
                      <tr style={{ borderBottom: r.prediction ? 'none' : '1px solid rgba(255,255,255,0.05)', transition: 'var(--transition)' }}>
                        <td style={{ padding: '16px 12px' }}>
                          <div style={{ fontWeight: 'bold' }}>{r.courseCode}</div>
                          <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>{r.courseName}</div>
                        </td>
                        <td style={{ padding: '16px 12px' }}>{r.creditHours}</td>
                        <td style={{ padding: '16px 12px' }}>{r.published && r.week7Score !== null && r.week7Score !== undefined ? r.week7Score : '-'}</td>
                        <td style={{ padding: '16px 12px' }}>{r.published && r.week12Score !== null && r.week12Score !== undefined ? r.week12Score : '-'}</td>
                        <td style={{ padding: '16px 12px' }}>{r.published && r.prefinalScore !== null && r.prefinalScore !== undefined ? r.prefinalScore : '-'}</td>
                        <td style={{ padding: '16px 12px' }}>{r.published && r.finalScore !== null && r.finalScore !== undefined ? r.finalScore : '-'}</td>
                        <td style={{ padding: '16px 12px', fontWeight: 'bold', color: 'var(--accent)' }}>
                          {r.published && r.totalScore !== null && r.totalScore !== undefined ? r.totalScore : '-'}
                        </td>
                        <td style={{ padding: '16px 12px' }}>
                          {r.published && r.letterGrade ? (
                            <span style={{
                              padding: '4px 8px',
                              background: r.letterGrade.startsWith('F') ? 'rgba(239, 68, 68, 0.2)' : 'rgba(16, 185, 129, 0.2)',
                              border: r.letterGrade.startsWith('F') ? '1px solid var(--error)' : '1px solid var(--success)',
                              color: r.letterGrade.startsWith('F') ? 'var(--error)' : 'var(--success)',
                              borderRadius: '4px',
                              fontSize: '0.85rem',
                              fontWeight: 'bold'
                            }}>
                              {r.letterGrade}
                            </span>
                          ) : (
                            <span style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>Pending</span>
                          )}
                        </td>
                      </tr>
                      {r.prediction && (
                        <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                          <td colSpan={8} style={{ padding: '0 12px 12px 12px' }}>
                            <div style={{
                              padding: '8px 12px',
                              background: r.prediction.atRisk ? 'rgba(239,68,68,0.1)' : 'rgba(59,130,246,0.1)',
                              border: `1px solid ${r.prediction.atRisk ? '#EF4444' : '#3B82F6'}`,
                              borderRadius: '8px',
                              display: 'flex',
                              alignItems: 'center',
                              gap: '8px'
                            }}>
                              <span style={{ fontSize: '16px' }}>
                                {r.prediction.atRisk ? '⚠️' : '📊'}
                              </span>
                              <div>
                                <span style={{
                                  fontSize: '12px',
                                  color: r.prediction.atRisk ? '#EF4444' : '#60A5FA',
                                  fontWeight: 'bold'
                                }}>
                                  AI Prediction
                                </span>
                                <span style={{ fontSize: '13px', color: '#CBD5E1', marginLeft: '8px' }}>
                                  Predicted Final: {r.prediction.finalRange.low}–{r.prediction.finalRange.high} / 40
                                </span>
                                <span style={{ fontSize: '13px', color: '#CBD5E1', marginLeft: '8px' }}>
                                  Predicted Total: {r.prediction.totalRange.low}–{r.prediction.totalRange.high} / 100
                                </span>
                                {r.prediction.atRisk && (
                                  <span style={{
                                    fontSize: '11px', color: '#EF4444',
                                    marginLeft: '8px', fontWeight: 'bold'
                                  }}>
                                    ⚠ At risk of Auto-F — predicted final below 12
                                  </span>
                                )}
                              </div>
                            </div>
                          </td>
                        </tr>
                      )}
                    </React.Fragment>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        ))
      )}
    </div>
  );
};
