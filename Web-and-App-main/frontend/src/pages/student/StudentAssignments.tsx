import React, { useEffect, useState } from 'react';
import apiClient from '../../api/apiClient';

export const StudentAssignments: React.FC = () => {
  const [assignments, setAssignments] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    load();
  }, []);

  async function load() {
    setLoading(true);
    try {
      const res = await apiClient.get('/assignments');
      setAssignments(res.data || []);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div>
      <h2>Assignments</h2>
      {loading ? (
        <div>Loading...</div>
      ) : assignments.length === 0 ? (
        <div>No assignments found.</div>
      ) : (
        <ul>
          {assignments.map((a) => (
            <li key={a.id} style={{ marginBottom: 16 }}>
              <div>
                <strong>{a.title}</strong>
                {a.dueDate ? <span style={{ marginLeft: 8, color: '#666' }}>Due: {new Date(a.dueDate).toLocaleDateString()}</span> : null}
              </div>
              <div>{a.body}</div>
              {a.attachments?.length ? (
                <div style={{ marginTop: 8 }}>
                  <div><strong>Attachments:</strong></div>
                  <ul>
                    {a.attachments.map((att: any) => (
                      <li key={att.id}>
                        <a href={`${apiClient.defaults.baseURL}/assignments/${a.id}/attachments/${att.id}`} target="_blank" rel="noreferrer">
                          {att.fileName}
                        </a>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : null}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};
