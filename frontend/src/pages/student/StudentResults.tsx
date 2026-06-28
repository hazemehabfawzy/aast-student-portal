import React, { useEffect, useState } from 'react';
import apiClient from '../../api/apiClient';

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
}

interface ResultsResponse {
  cumulativeGpa: number;
  academicStanding: string;
  results: StudentResult[];
}

export const StudentResults: React.FC = () => {
  const [results, setResults] = useState<StudentResult[]>([]);
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
      .catch((err) => {
        console.error(err);
        setError('Failed to retrieve academic results.');
      })
      .finally(() => {
        setLoading(false);
      });
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
                    <th style={{ padding: '16px 12px' }}>Week 7 / 30</th>
                    <th style={{ padding: '16px 12px' }}>Week 12 / 20</th>
                    <th style={{ padding: '16px 12px' }}>Prefinal / 10</th>
                    <th style={{ padding: '16px 12px' }}>Final / 40</th>
                    <th style={{ padding: '16px 12px' }}>Total / 100</th>
                    <th style={{ padding: '16px 12px' }}>Grade</th>
                  </tr>
                </thead>
                <tbody>
                  {semesterResults.map((r, i) => (
                    <tr key={i} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)', transition: 'var(--transition)' }}>
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
