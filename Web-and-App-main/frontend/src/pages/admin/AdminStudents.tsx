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

interface Department { id: string; name: string; }

const emptyForm = { firstName: '', lastName: '', studentNumber: '', email: '', username: '', password: '', departmentId: '', showPassword: false };
const emptyEditForm = { fullName: '', email: '', studentNumber: '', departmentId: '' };

export const AdminStudents: React.FC = () => {
  const [students, setStudents]         = useState<Student[]>([]);
  const [departments, setDepartments]   = useState<Department[]>([]);
  const [loading, setLoading]           = useState(true);
  const [search, setSearch]             = useState('');
  const [showModal, setShowModal]       = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [editingStudent, setEditingStudent] = useState<Student | null>(null);
  const [editForm, setEditForm]         = useState(emptyEditForm);
  const [form, setForm]                 = useState(emptyForm);
  const [creating, setCreating]         = useState(false);
  const [saving, setSaving]             = useState(false);
  const [successMsg, setSuccessMsg]     = useState('');
  const [errorMsg, setErrorMsg]         = useState('');

  const loadStudents = () =>
    apiClient.get<Student[]>('/students').then((res) => setStudents(res.data));

  useEffect(() => {
    Promise.all([
      loadStudents(),
      apiClient.get<Department[]>('/departments'),
    ]).then(([, dRes]) => {
      setDepartments(dRes.data);
    }).catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const filtered = students.filter(s =>
    s.fullName.toLowerCase().includes(search.toLowerCase()) ||
    s.studentNumber.toLowerCase().includes(search.toLowerCase()) ||
    s.email.toLowerCase().includes(search.toLowerCase())
  );

  const handleFirstLastChange = (field: 'firstName' | 'lastName', value: string) => {
    const updated = { ...form, [field]: value };
    const autoUser = `${updated.firstName.toLowerCase()}.${updated.lastName.toLowerCase()}`.replace(/\s+/g, '');
    setForm({ ...updated, username: autoUser });
  };

  const openEdit = (student: Student) => {
    setEditingStudent(student);
    setEditForm({
      fullName: student.fullName,
      email: student.email,
      studentNumber: student.studentNumber,
      departmentId: student.departmentId,
    });
    setErrorMsg('');
    setShowEditModal(true);
  };

  const handleSaveEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingStudent) return;
    setSaving(true);
    setErrorMsg('');
    try {
      await apiClient.put(`/students/${editingStudent.id}`, {
        fullName: editForm.fullName,
        email: editForm.email,
        studentNumber: editForm.studentNumber,
        departmentId: editForm.departmentId,
        yearLevel: editingStudent.yearLevel,
      });
      await loadStudents();
      setShowEditModal(false);
      setEditingStudent(null);
    } catch (err: any) {
      setErrorMsg(err.response?.data?.message ?? 'Failed to update student.');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (student: Student) => {
    if (!window.confirm('Are you sure? This cannot be undone.')) return;
    try {
      await apiClient.delete(`/students/${student.id}`);
      setStudents((prev) => prev.filter((s) => s.id !== student.id));
    } catch (err: any) {
      alert(err.response?.data?.message ?? 'Failed to delete student.');
    }
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreating(true);
    setSuccessMsg('');
    setErrorMsg('');
    try {
      const payload = {
        username: form.username,
        password: form.password,
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email,
        studentNumber: form.studentNumber,
        departmentId: form.departmentId,
      };
      const res = await apiClient.post('/students/create', payload);
      setSuccessMsg(res.data.message ?? `Account created: ${form.username}`);
      setStudents(prev => [...prev, { id: res.data.id, fullName: res.data.fullName, email: form.email, studentNumber: form.studentNumber, departmentId: form.departmentId, yearLevel: 1 }]);
      setForm(emptyForm);
    } catch (err: any) {
      setErrorMsg(err.response?.data?.error ?? err.response?.data?.message ?? 'Failed to create account.');
    } finally {
      setCreating(false);
    }
  };

  if (loading) return <div className="brand-subtitle">Loading student directory...</div>;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <h1 style={{ marginBottom: '8px' }}>Students Directory</h1>
          <p style={{ color: 'var(--text-muted)' }}>Search and manage student enrollment accounts.</p>
        </div>
        <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap' }}>
          <input type="text" className="form-input" placeholder="🔍 Search name, email, ID..." style={{ width: '260px' }} value={search} onChange={e => setSearch(e.target.value)} />
          <button className="glass-btn primary" onClick={() => { setShowModal(true); setSuccessMsg(''); setErrorMsg(''); }}>➕ Create Account</button>
        </div>
      </div>

      <div className="glass-panel" style={{ padding: '24px', overflowX: 'auto' }}>
        {filtered.length === 0
          ? <p style={{ color: 'var(--text-muted)', textAlign: 'center' }}>No students found.</p>
          : (
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)', fontSize: '0.9rem' }}>
                  <th style={{ padding: '16px 12px' }}>Student Number</th>
                  <th style={{ padding: '16px 12px' }}>Full Name</th>
                  <th style={{ padding: '16px 12px' }}>Email Address</th>
                  <th style={{ padding: '16px 12px' }}>Year Level</th>
                  <th style={{ padding: '16px 12px' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map(s => (
                  <tr key={s.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                    <td style={{ padding: '16px 12px', fontWeight: 'bold', color: 'var(--accent)' }}>{s.studentNumber}</td>
                    <td style={{ padding: '16px 12px' }}>{s.fullName}</td>
                    <td style={{ padding: '16px 12px' }}>{s.email}</td>
                    <td style={{ padding: '16px 12px' }}>Year {s.yearLevel}</td>
                    <td style={{ padding: '16px 12px' }}>
                      <div style={{ display: 'flex', gap: '8px' }}>
                        <button className="glass-btn" style={{ padding: '6px 12px', fontSize: '0.85rem' }} onClick={() => openEdit(s)}>✏️ Edit</button>
                        <button className="glass-btn" style={{ padding: '6px 12px', fontSize: '0.85rem', borderColor: 'var(--error)', color: 'var(--error)' }} onClick={() => handleDelete(s)}>🗑️ Delete</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )
        }
      </div>

      {/* Edit Student Modal */}
      {showEditModal && editingStudent && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.85)', zIndex: 1000, display: 'flex', justifyContent: 'center', alignItems: 'center', padding: '20px' }}>
          <div className="glass-panel" style={{ width: '100%', maxWidth: '460px', padding: '32px', background: '#1e293b' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
              <h2>Edit Student</h2>
              <button className="glass-btn" onClick={() => setShowEditModal(false)}>✕</button>
            </div>
            {errorMsg && (
              <div style={{ background: 'rgba(239,68,68,0.15)', border: '1px solid #EF4444', borderRadius: '8px', padding: '12px 16px', marginBottom: '16px', color: '#EF4444' }}>
                ❌ {errorMsg}
              </div>
            )}
            <form onSubmit={handleSaveEdit} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
              <div className="form-group">
                <label className="form-label">Full Name</label>
                <input className="form-input" required value={editForm.fullName} onChange={e => setEditForm({ ...editForm, fullName: e.target.value })} />
              </div>
              <div className="form-group">
                <label className="form-label">Email</label>
                <input className="form-input" type="email" required value={editForm.email} onChange={e => setEditForm({ ...editForm, email: e.target.value })} />
              </div>
              <div className="form-group">
                <label className="form-label">Student Number</label>
                <input className="form-input" required value={editForm.studentNumber} onChange={e => setEditForm({ ...editForm, studentNumber: e.target.value })} />
              </div>
              <div className="form-group">
                <label className="form-label">Department</label>
                <select className="form-input" style={{ background: 'rgba(15,23,42,0.9)' }} required value={editForm.departmentId} onChange={e => setEditForm({ ...editForm, departmentId: e.target.value })}>
                  <option value="">— Select Department —</option>
                  {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
                </select>
              </div>
              <div style={{ display: 'flex', gap: '12px', marginTop: '8px' }}>
                <button type="button" className="glass-btn" style={{ flex: 1, justifyContent: 'center' }} onClick={() => setShowEditModal(false)}>Cancel</button>
                <button type="submit" className="glass-btn primary" style={{ flex: 1, justifyContent: 'center' }} disabled={saving}>{saving ? 'Saving...' : 'Save Changes'}</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Create Student Account Modal */}
      {showModal && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.85)', zIndex: 1000, display: 'flex', justifyContent: 'center', alignItems: 'center', padding: '20px' }}>
          <div className="glass-panel" style={{ width: '100%', maxWidth: '460px', padding: '32px', background: '#1e293b' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
              <h2>Create Student Account</h2>
              <button className="glass-btn" onClick={() => setShowModal(false)}>✕</button>
            </div>

            {successMsg && (
              <div style={{ background: 'rgba(16,185,129,0.15)', border: '1px solid #10B981', borderRadius: '8px', padding: '12px 16px', marginBottom: '16px', color: '#10B981', fontWeight: 'bold' }}>
                ✅ {successMsg}
              </div>
            )}
            {errorMsg && (
              <div style={{ background: 'rgba(239,68,68,0.15)', border: '1px solid #EF4444', borderRadius: '8px', padding: '12px 16px', marginBottom: '16px', color: '#EF4444' }}>
                ❌ {errorMsg}
              </div>
            )}

            <form onSubmit={handleCreate} style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '14px' }}>
                <div className="form-group">
                  <label className="form-label">First Name</label>
                  <input className="form-input" required value={form.firstName} onChange={e => handleFirstLastChange('firstName', e.target.value)} />
                </div>
                <div className="form-group">
                  <label className="form-label">Last Name</label>
                  <input className="form-input" required value={form.lastName} onChange={e => handleFirstLastChange('lastName', e.target.value)} />
                </div>
              </div>
              <div className="form-group">
                <label className="form-label">Student Number</label>
                <input className="form-input" required placeholder="e.g. 19104001" value={form.studentNumber} onChange={e => setForm({ ...form, studentNumber: e.target.value })} />
              </div>
              <div className="form-group">
                <label className="form-label">Email</label>
                <input className="form-input" type="email" required value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} />
              </div>
              <div className="form-group">
                <label className="form-label">Username</label>
                <input className="form-input" required value={form.username} onChange={e => setForm({ ...form, username: e.target.value })} />
              </div>
              <div className="form-group">
                <label className="form-label">Password</label>
                <div style={{ position: 'relative' }}>
                  <input className="form-input" required type={form.showPassword ? 'text' : 'password'} style={{ paddingRight: '44px', width: '100%' }} value={form.password} onChange={e => setForm({ ...form, password: e.target.value })} />
                  <button type="button" onClick={() => setForm({ ...form, showPassword: !form.showPassword })} style={{ position: 'absolute', right: '10px', top: '50%', transform: 'translateY(-50%)', background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-muted)' }}>{form.showPassword ? '🙈' : '👁️'}</button>
                </div>
              </div>
              <div className="form-group">
                <label className="form-label">Department</label>
                <select className="form-input" style={{ background: 'rgba(15,23,42,0.9)' }} required value={form.departmentId} onChange={e => setForm({ ...form, departmentId: e.target.value })}>
                  <option value="">— Select Department —</option>
                  {departments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
                </select>
              </div>
              <div style={{ display: 'flex', gap: '12px', marginTop: '8px' }}>
                <button type="button" className="glass-btn" style={{ flex: 1, justifyContent: 'center' }} onClick={() => setShowModal(false)}>Cancel</button>
                <button type="submit" className="glass-btn primary" style={{ flex: 1, justifyContent: 'center' }} disabled={creating}>{creating ? 'Creating...' : 'Create Account'}</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
