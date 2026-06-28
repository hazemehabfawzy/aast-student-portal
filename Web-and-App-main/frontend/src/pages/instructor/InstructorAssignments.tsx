import React, { useEffect, useState } from 'react';
import apiClient from '../../api/apiClient';

export const InstructorAssignments: React.FC = () => {
  const [assignments, setAssignments] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [title, setTitle] = useState('');
  const [body, setBody] = useState('');
  const [selectedFiles, setSelectedFiles] = useState<Record<number, File | null>>({});

  useEffect(() => { load(); }, []);

  async function load() {
    setLoading(true);
    try {
      const res = await apiClient.get('/assignments');
      setAssignments(res.data || []);
    } catch (e) {
      console.error(e);
    } finally { setLoading(false); }
  }

  async function uploadAttachment(assignmentId: number) {
    const file = selectedFiles[assignmentId];
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);

    try {
      await apiClient.post(`/assignments/${assignmentId}/attachments`, formData);
      setSelectedFiles((prev) => ({ ...prev, [assignmentId]: null }));
      await load();
    } catch (e) {
      console.error(e);
    }
  }

  async function create() {
    try {
      await apiClient.post('/assignments', { title, body });
      setTitle(''); setBody('');
      await load();
    } catch (e) { console.error(e); }
  }

  return (
    <div>
      <h2>Instructor Assignments</h2>
      <div style={{ marginBottom: 16 }}>
        <input placeholder="Title" value={title} onChange={e => setTitle(e.target.value)} />
        <input placeholder="Body" value={body} onChange={e => setBody(e.target.value)} />
        <button onClick={create}>Create</button>
      </div>

      {loading ? <div>Loading...</div> : (
        <ul>
          {assignments.map(a => (
            <li key={a.id} style={{ marginBottom: 24 }}>
              <div>
                <strong>{a.title}</strong>
                {a.dueDate ? <span style={{ marginLeft: 8, color: '#666' }}>Due: {new Date(a.dueDate).toLocaleDateString()}</span> : null}
              </div>
              <div>{a.body}</div>
              {a.attachments?.length ? (
                <div style={{ marginTop: 8 }}>
                  <strong>Attachments:</strong>
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
              <div style={{ marginTop: 8, display: 'flex', gap: 8, alignItems: 'center' }}>
                <input
                  type="file"
                  onChange={(e) => {
                    const file = e.target.files?.[0] ?? null;
                    setSelectedFiles((prev) => ({ ...prev, [a.id]: file }));
                  }}
                />
                <button onClick={() => uploadAttachment(a.id)} disabled={!selectedFiles[a.id]}>Upload</button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};
