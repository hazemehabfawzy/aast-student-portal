import React, { useEffect, useState } from 'react';
import apiClient from '../../api/apiClient';

interface Instructor { id: string; fullName: string; keycloakId?: string; departmentId: string; departmentName?: string; }
interface Department  { id: string; name: string; }

const emptyForm = { firstName: '', lastName: '', email: '', username: '', password: '', departmentId: '', showPassword: false };
const emptyEditForm = { fullName: '', departmentId: '' };

export const AdminInstructors: React.FC = () => {
  const [instructors, setInstructors]   = useState<Instructor[]>([]);
  const [departments, setDepartments]   = useState<Department[]>([]);
  const [loading, setLoading]           = useState(true);
  const [search, setSearch]             = useState('');
  const [showModal, setShowModal]       = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [editingInstructor, setEditingInstructor] = useState<Instructor | null>(null);
  const [editForm, setEditForm]         = useState(emptyEditForm);
  const [form, setForm]                 = useState(emptyForm);
  const [creating, setCreating]         = useState(false);
  const [saving, setSaving]             = useState(false);
  const [successMsg, setSuccessMsg]     = useState('');
  const [errorMsg, setErrorMsg]         = useState('');

  const loadInstructors = () =>
    apiClient.get<Instructor[]>('/admin/instructors').then((res) => setInstructors(res.data));

  useEffect(() => {
    Promise.all([
      loadInstructors(),
      apiClient.get<Department[]>('/departments'),
    ]).then(([, dRes]) => {
      setDepartments(dRes.data);
    }).catch(() => {})
      .finally(() => setLoading(false));
  }, []);

  const filtered = instructors.filter(i =>
    i.fullName.toLowerCase().includes(search.toLowerCase())
  );

  const handleFirstLastChange = (field: 'firstName' | 'lastName', value: string) => {
    const updated = { ...form, [field]: value };
    const autoUser = `${updated.firstName.toLowerCase()}.${updated.lastName.toLowerCase()}`.replace(/\s+/g, '');
    setForm({ ...updated, username: autoUser });
  };

  const openEdit = (instructor: Instructor) => {
    setEditingInstructor(instructor);
    setEditForm({
      fullName: instructor.fullName,
      departmentId: instructor.departmentId,
    });
    setErrorMsg('');
    setShowEditModal(true);
  };

  const handleSaveEdit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingInstructor) return;
    setSaving(true);
    setErrorMsg('');
    try {
      await apiClient.put(`/instructors/${editingInstructor.id}`, {
        fullName: editForm.fullName,
        departmentId: editForm.departmentId,
      });
      await loadInstructors();
      setShowEditModal(false);
      setEditingInstructor(null);
    } catch (err: any) {
      setErrorMsg(err.response?.data?.message ?? 'Failed to update instructor.');
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (instructor: Instructor) => {
    if (!window.confirm('Are you sure? This cannot be undone.')) return;
    try {
      await apiClient.delete(`/instructors/${instructor.id}`);
      setInstructors((prev) => prev.filter((i) => i.id !== instructor.id));
    } catch (err: any) {
      alert(err.response?.data?.message ?? 'Failed to delete instructor.');
    }
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreating(true);
    setSuccessMsg('');
    setErrorMsg('');
    try {
      const payload = { username: form.username, password: form.password, firstName: form.firstName, lastName: form.lastName, email: form.email, departmentId: form.departmentId };
      const res = await apiClient.post('/instructors/create', payload);
      setSuccessMsg(res.data.message ?? `Account created: ${form.username}`);
      setInstructors(prev => [...prev, { id: res.data.id, fullName: res.data.fullName, keycloakId: res.data.keycloakId, departmentId: form.departmentId }]);
      setForm(emptyForm);
    } catch (err: any) {
      setErrorMsg(err.response?.data?.error ?? err.response?.data?.message ?? 'Failed to create account.');
    } finally {
      setCreating(false);
    }
  };

  if (loading) return <div className="brand-subtitle">Loading instructors...</div>;

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <h1 style={{ marginBottom: '8px' }}>Instructors Directory</h1>
          <p style={{ color: 'var(--text-muted)' }}>Manage instructor accounts and assignments.</p>
        </div>
        <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap' }}>
          <input type="text" className="form-input" placeholder="🔍 Search name..." style={{ width: '240px' }} value={search} onChange={e => setSearch(e.target.value)} />
          <button className="glass-btn primary" onClick={() => { setShowModal(true); setSuccessMsg(''); setErrorMsg(''); }}>➕ Create Account</button>
        </div>
      </div>

      <div className="glass-panel" style={{ padding: '24px', overflowX: 'auto' }}>
        {filtered.length === 0
          ? <p style={{ color: 'var(--text-muted)', textAlign: 'center' }}>No instructors found.</p>
          : (
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)', fontSize: '0.9rem' }}>
                  <th style={{ padding: '16px 12px' }}>Full Name</th>
                  <th style={{ padding: '16px 12px' }}>Department</th>
                  <th style={{ padding: '16px 12px' }}>Keycloak ID</th>
                  <th style={{ padding: '16px 12px' }}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map(i => (
                  <tr key={i.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                    <td style={{ padding: '16px 12px', fontWeight: 'bold' }}>{i.fullName}</td>
                    <td style={{ padding: '16px 12px' }}>{i.departmentName ?? departments.find(d => d.id === i.departmentId)?.name ?? 'N/A'}</td>
                    <td style={{ padding: '16px 12px', fontSize: '0.8rem', color: 'var(--text-muted)' }}>{i.keycloakId ?? '—'}</td>
                    <td style={{ padding: '16px 12px' }}>
                      <div style={{ display: 'flex', gap: '8px' }}>
                        <button className="glass-btn" style={{ padding: '6px 12px', fontSize: '0.85rem' }} onClick={() => openEdit(i)}>✏️ Edit</button>
                        <button className="glass-btn" style={{ padding: '6px 12px', fontSize: '0.85rem', borderColor: 'var(--error)', color: 'var(--error)' }} onClick={() => handleDelete(i)}>🗑️ Delete</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )
        }
      </div>

      {/* Edit Instructor Modal */}
      {showEditModal && editingInstructor && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.85)', zIndex: 1000, display: 'flex', justifyContent: 'center', alignItems: 'center', padding: '20px' }}>
          <div className="glass-panel" style={{ width: '100%', maxWidth: '440px', padding: '32px', background: '#1e293b' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
              <h2>Edit Instructor</h2>
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

      {/* Create Instructor Account Modal */}
      {showModal && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.85)', zIndex: 1000, display: 'flex', justifyContent: 'center', alignItems: 'center', padding: '20px' }}>
          <div className="glass-panel" style={{ width: '100%', maxWidth: '440px', padding: '32px', background: '#1e293b' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
              <h2>Create Instructor Account</h2>
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
