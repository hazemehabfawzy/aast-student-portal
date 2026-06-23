import React, { useEffect, useState } from 'react';
import apiClient from '../../api/apiClient';

interface Student {
  id: string;
  studentNumber: string;
  fullName: string;
  email: string;
  departmentId: string;
  yearLevel: number;
}

export const AdminStudents: React.FC = () => {
  const [students, setStudents] = useState<Student[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');

  useEffect(() => {
    apiClient.get<Student[]>('/students')
      .then((res) => {
        setStudents(res.data);
      })
      .catch((err) => console.error('Failed to load students', err))
      .finally(() => setLoading(false));
  }, []);

  const filtered = students.filter(s =>
    s.fullName.toLowerCase().includes(search.toLowerCase()) ||
    s.studentNumber.toLowerCase().includes(search.toLowerCase()) ||
    s.email.toLowerCase().includes(search.toLowerCase())
  );

  if (loading) {
    return <div className="brand-subtitle">Loading student directory...</div>;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <h1 style={{ marginBottom: '8px' }}>Students Directory</h1>
          <p style={{ color: 'var(--text-muted)' }}>Search and view local student enrollment accounts.</p>
        </div>
        <div>
          <input
            type="text"
            className="form-input"
            placeholder="🔍 Search name, email, ID..."
            style={{ width: '280px' }}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
      </div>

      <div className="glass-panel" style={{ padding: '24px', overflowX: 'auto' }}>
        {filtered.length === 0 ? (
          <p style={{ color: 'var(--text-muted)', textAlign: 'center' }}>No students found matching search criteria.</p>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)', fontSize: '0.9rem' }}>
                <th style={{ padding: '16px 12px' }}>Student Number</th>
                <th style={{ padding: '16px 12px' }}>Full Name</th>
                <th style={{ padding: '16px 12px' }}>Email Address</th>
                <th style={{ padding: '16px 12px' }}>Year Level</th>
                <th style={{ padding: '16px 12px' }}>Department</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((s) => (
                <tr key={s.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                  <td style={{ padding: '16px 12px', fontWeight: 'bold', color: 'var(--accent)' }}>{s.studentNumber}</td>
                  <td style={{ padding: '16px 12px' }}>{s.fullName}</td>
                  <td style={{ padding: '16px 12px' }}>{s.email}</td>
                  <td style={{ padding: '16px 12px' }}>Year {s.yearLevel}</td>
                  <td style={{ padding: '16px 12px' }}>
                    {s.departmentId ? 'Computer Engineering' : 'N/A'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};
