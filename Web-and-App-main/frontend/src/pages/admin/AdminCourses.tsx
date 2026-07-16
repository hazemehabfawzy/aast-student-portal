import React, { useEffect, useState } from 'react';
import apiClient from '../../api/apiClient';

interface Course {
  id: string;
  code: string;
  name: string;
  creditHours: number;
  departmentId: string;
  semesterNumber: number;
  prerequisiteCode: string;
}

export const AdminCourses: React.FC = () => {
  const [courses, setCourses] = useState<Course[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [departments, setDepartments] = useState<{ id: string; name: string }[]>([]);

  // Form State
  const [editingCourse, setEditingCourse] = useState<Course | null>(null);
  const [isNew, setIsNew] = useState(false);
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [creditHours, setCreditHours] = useState<number>(3);
  const [departmentId, setDepartmentId] = useState<string>('');
  const [semesterNumber, setSemesterNumber] = useState<number>(1);
  const [prerequisiteCode, setPrerequisiteCode] = useState('');

  const fetchCourses = () => {
    setLoading(true);
    apiClient.get<Course[]>('/courses')
      .then((res) => {
        setCourses(res.data);
      })
      .catch(() => {
        setError('Failed to load courses.');
      })
      .finally(() => setLoading(false));
  };

  const fetchDepartments = () => {
    apiClient.get<{ id: string; name: string }[]>('/departments')
      .then((res) => {
        setDepartments(res.data);
      })
      .catch(() => {
      });
  };

  useEffect(() => {
    fetchCourses();
    fetchDepartments();
  }, []);

  const openNewForm = () => {
    setIsNew(true);
    setEditingCourse({
      id: '',
      code: '',
      name: '',
      creditHours: 3,
      departmentId: '',
      semesterNumber: 1,
      prerequisiteCode: ''
    });
    setCode('');
    setName('');
    setCreditHours(3);
    setDepartmentId('');
    setSemesterNumber(1);
    setPrerequisiteCode('');
  };

  const openEditForm = (course: Course) => {
    setIsNew(false);
    setEditingCourse(course);
    setCode(course.code);
    setName(course.name);
    setCreditHours(course.creditHours);
    setDepartmentId(course.departmentId);
    setSemesterNumber(course.semesterNumber);
    setPrerequisiteCode(course.prerequisiteCode || '');
  };

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingCourse) return;

    const payload = {
      id: editingCourse.id,
      code,
      name,
      creditHours,
      departmentId,
      semesterNumber,
      prerequisiteCode: prerequisiteCode === '' ? '' : prerequisiteCode
    };

    const request = isNew
      ? apiClient.post('/courses', payload)
      : apiClient.put(`/courses/${editingCourse.id}`, payload);

    request
      .then(() => {
        alert(isNew ? 'Course created successfully!' : 'Course updated successfully!');
        setEditingCourse(null);
        fetchCourses();
      })
      .catch((err) => {
        alert(err.response?.data?.message || 'Failed to save course.');
      });
  };

  const handleDelete = (id: string) => {
    if (!window.confirm('Are you sure you want to soft-delete this course? All historical records remain intact.')) return;
    apiClient.delete(`/courses/${id}`)
      .then(() => {
        alert('Course soft-deleted successfully.');
        fetchCourses();
      })
      .catch((err) => {
        alert(err.response?.data?.message || 'Failed to soft-delete course.');
      });
  };

  if (loading && courses.length === 0) {
    return <div className="brand-subtitle">Loading course catalog...</div>;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <h1 style={{ marginBottom: '8px' }}>Course Catalog</h1>
          <p style={{ color: 'var(--text-muted)' }}>Manage academic curriculums and prerequisites.</p>
        </div>
        <button className="glass-btn primary" onClick={openNewForm}>
          ➕ Add Course
        </button>
      </div>

      {error ? (
        <div style={{ color: 'var(--error)' }}>{error}</div>
      ) : (
        <div className="glass-panel" style={{ padding: '24px', overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)', fontSize: '0.9rem' }}>
                <th style={{ padding: '16px 12px' }}>Code</th>
                <th style={{ padding: '16px 12px' }}>Course Name</th>
                <th style={{ padding: '16px 12px' }}>Credits</th>
                <th style={{ padding: '16px 12px' }}>Term</th>
                <th style={{ padding: '16px 12px' }}>Prerequisite</th>
                <th style={{ padding: '16px 12px', textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {[...courses].sort((a, b) => a.semesterNumber !== b.semesterNumber ? a.semesterNumber - b.semesterNumber : a.code.localeCompare(b.code)).map((course) => (
                <tr key={course.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                  <td style={{ padding: '16px 12px', fontWeight: 'bold', color: 'var(--accent)' }}>{course.code}</td>
                  <td style={{ padding: '16px 12px' }}>{course.name}</td>
                  <td style={{ padding: '16px 12px' }}>{course.creditHours} Hrs</td>
                  <td style={{ padding: '16px 12px' }}>Sem {course.semesterNumber}</td>
                  <td style={{ padding: '16px 12px', color: 'var(--text-muted)' }}>
                    {course.prerequisiteCode || 'None'}
                  </td>
                  <td style={{ padding: '16px 12px', textAlign: 'right' }}>
                    <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                      <button className="glass-btn" style={{ padding: '6px 12px', fontSize: '0.85rem' }} onClick={() => openEditForm(course)}>
                        ✏️ Edit
                      </button>
                      <button className="glass-btn" style={{ padding: '6px 12px', fontSize: '0.85rem', borderColor: 'var(--error)', color: 'var(--error)' }} onClick={() => handleDelete(course.id)}>
                        🗑️ Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {editingCourse && (
        <div style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          background: 'rgba(0,0,0,0.8)',
          zIndex: 1000,
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          padding: '20px'
        }}>
          <div className="glass-panel" style={{ width: '100%', maxWidth: '500px', padding: '32px', background: '#1e293b' }}>
            <h2 style={{ marginBottom: '16px' }}>{isNew ? 'Create Course' : 'Edit Course'}</h2>
            <form onSubmit={handleSave} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div className="form-group">
                <label className="form-label">Course Code</label>
                <input
                  type="text"
                  className="form-input"
                  required
                  value={code}
                  onChange={(e) => setCode(e.target.value)}
                />
              </div>

              <div className="form-group">
                <label className="form-label">Course Name</label>
                <input
                  type="text"
                  className="form-input"
                  required
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                />
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group">
                  <label className="form-label">Credit Hours</label>
                  <input
                    type="number"
                    className="form-input"
                    min={1}
                    max={6}
                    required
                    value={creditHours}
                    onChange={(e) => setCreditHours(Number(e.target.value))}
                  />
                </div>
                <div className="form-group">
                  <label className="form-label">Semester Number</label>
                  <input
                    type="number"
                    className="form-input"
                    min={1}
                    max={10}
                    required
                    value={semesterNumber}
                    onChange={(e) => setSemesterNumber(Number(e.target.value))}
                  />
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group">
                  <label className="form-label">Department</label>
                  <select
                    className="form-input"
                    required
                    value={departmentId}
                    onChange={(e) => setDepartmentId(e.target.value)}
                    style={{ background: '#0f172a', color: '#f8fafc', width: '100%', height: '38px', borderRadius: '6px', border: '1px solid var(--border-color)', padding: '0 12px' }}
                  >
                    <option value="">Select Department</option>
                    {departments.map((dept) => (
                      <option key={dept.id} value={dept.id}>
                        {dept.name}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">Prerequisite Code</label>
                  <input
                    type="text"
                    className="form-input"
                    value={prerequisiteCode}
                    placeholder="e.g. CC111 (Optional)"
                    onChange={(e) => setPrerequisiteCode(e.target.value)}
                  />
                </div>
              </div>

              <div style={{ display: 'flex', gap: '16px', marginTop: '16px' }}>
                <button
                  type="button"
                  className="glass-btn"
                  style={{ flex: 1, justifyContent: 'center' }}
                  onClick={() => setEditingCourse(null)}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="glass-btn primary"
                  style={{ flex: 1, justifyContent: 'center' }}
                >
                  Save Course
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
