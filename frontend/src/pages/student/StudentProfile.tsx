import React, { useEffect, useState } from 'react';
import { useAuth } from '../../auth/AuthContext';
import apiClient from '../../api/apiClient';

interface StudentResult {
  courseCode: string;
  courseName: string;
  creditHours: number;
  totalScore?: number;
  letterGrade?: string;
}

export const StudentProfile: React.FC = () => {
  const { fullName, email, username } = useAuth();
  const [results, setResults] = useState<StudentResult[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    apiClient.get<{ results: StudentResult[] }>('/students/me/results')
      .then((res) => {
        setResults(res.data.results);
      })
      .catch((err) => {
        console.error('Failed to load profile academic data', err);
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  const totalCredits = results.reduce((acc, r) => acc + r.creditHours, 0);
  
  // Calculate a simple GPA from letter grades
  const getGradePoints = (grade?: string): number => {
    if (!grade) return 0;
    switch (grade.toUpperCase()) {
      case 'A+': case 'A': return 4.0;
      case 'A-': return 3.7;
      case 'B+': return 3.3;
      case 'B': return 3.0;
      case 'B-': return 2.7;
      case 'C+': return 2.3;
      case 'C': return 2.0;
      case 'C-': return 1.7;
      case 'D+': return 1.3;
      case 'D': return 1.0;
      default: return 0.0;
    }
  };

  const gradedCourses = results.filter(r => r.letterGrade);
  const totalGradePoints = gradedCourses.reduce((acc, r) => acc + (getGradePoints(r.letterGrade) * r.creditHours), 0);
  const totalGradedCredits = gradedCourses.reduce((acc, r) => acc + r.creditHours, 0);
  const gpa = totalGradedCredits > 0 ? (totalGradePoints / totalGradedCredits).toFixed(2) : 'N/A';

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px', display: 'flex', gap: '24px', alignItems: 'center', flexWrap: 'wrap' }}>
        <div style={{
          width: '80px',
          height: '80px',
          borderRadius: '50%',
          background: 'linear-gradient(to right, #60a5fa, var(--accent))',
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          fontSize: '2rem',
          fontWeight: 'bold',
          color: '#fff'
        }}>
          {fullName ? fullName.charAt(0) : 'S'}
        </div>
        <div>
          <h1 style={{ margin: 0, fontSize: '2rem' }}>{fullName}</h1>
          <p style={{ color: 'var(--text-muted)' }}>Student ID: {username} | {email}</p>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '24px' }}>
        <div className="glass-panel" style={{ padding: '24px' }}>
          <h3 style={{ marginBottom: '16px', color: 'var(--accent)' }}>🎓 Academic Summary</h3>
          <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--text-muted)' }}>Department:</span>
              <span style={{ fontWeight: 600 }}>Computer Engineering</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--text-muted)' }}>Advisor:</span>
              <span style={{ fontWeight: 600 }}>Dr. Ahmed Khalil</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--text-muted)' }}>Registered Credits:</span>
              <span style={{ fontWeight: 600 }}>{loading ? '...' : totalCredits} Credits</span>
            </div>
            <div style={{ display: 'flex', justifyContent: 'space-between' }}>
              <span style={{ color: 'var(--text-muted)' }}>Estimated Cumulative GPA:</span>
              <span style={{ fontWeight: 600, color: 'var(--success)' }}>{loading ? '...' : gpa}</span>
            </div>
          </div>
        </div>

        <div className="glass-panel" style={{ padding: '24px' }}>
          <h3 style={{ marginBottom: '16px', color: 'var(--accent)' }}>🛠️ Device & Platform</h3>
          <p style={{ color: 'var(--text-muted)', marginBottom: '16px', fontSize: '0.9rem' }}>
            Check-ins are allowed exclusively from verified mobile platforms.
          </p>
          <div style={{ padding: '12px', background: 'rgba(239, 68, 68, 0.08)', border: '1px solid rgba(239, 68, 68, 0.2)', borderRadius: '8px' }}>
            <div style={{ color: 'var(--error)', fontWeight: 600, marginBottom: '4px' }}>Web Browser Session</div>
            <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
              Check-in button is disabled on desktop. Please use the mobile application.
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
