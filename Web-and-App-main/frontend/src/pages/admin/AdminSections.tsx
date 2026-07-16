import React, { useEffect, useState } from 'react';
import apiClient from '../../api/apiClient';

const PERIODS = [
  { number: 1, start: '8:30 AM',  end: '10:00 AM' },
  { number: 2, start: '10:30 AM', end: '12:00 PM' },
  { number: 3, start: '12:30 PM', end: '2:00 PM'  },
  { number: 4, start: '2:30 PM',  end: '4:00 PM'  },
  { number: 5, start: '4:30 PM',  end: '6:00 PM'  },
  { number: 6, start: '6:30 PM',  end: '8:00 PM'  },
];

interface Section {
  id: string;
  courseId: string;
  instructorId: string;
  semesterId: string;
  scheduleJson: string;
  capacity: number;
  courseCode?: string;
  courseName?: string;
  instructorName?: string;
  studentCount?: number;
}

interface Course    { id: string; code: string; name: string; }
interface Instructor { id: string; fullName: string; }
interface Semester  { id: string; name: string; isCurrent: boolean; }
interface StudentItem { id: string; studentId: string; fullName: string; studentNumber: string; email: string; }
interface AllStudent  { id: string; fullName: string; studentNumber: string; email: string; }

export const AdminSections: React.FC = () => {
  const [sections, setSections]       = useState<Section[]>([]);
  const [courses, setCourses]         = useState<Course[]>([]);
  const [instructors, setInstructors] = useState<Instructor[]>([]);
  const [semesters, setSemesters]     = useState<Semester[]>([]);
  const [loading, setLoading]         = useState(true);

  // Edit / create form
  const [editingSection, setEditingSection] = useState<Section | null>(null);
  const [isNew, setIsNew]         = useState(false);
  const [courseId, setCourseId]   = useState<string>('');
  const [instructorId, setInstructorId] = useState<string>('');
  const [semesterId, setSemesterId]     = useState<string>('');
  const [capacity, setCapacity]         = useState<number>(30);
  const [scheduleJson, setScheduleJson] = useState('[]');

  // Schedule builder
  const [builderDay, setBuilderDay]     = useState('Sun');
  const [builderPeriod, setBuilderPeriod] = useState(1);
  const [builderRoom, setBuilderRoom]   = useState('');

  // Manage Students
  const [managingSectionId, setManagingSectionId]     = useState<string | null>(null);
  const [managingSectionName, setManagingSectionName] = useState('');
  const [enrolledStudents, setEnrolledStudents]       = useState<StudentItem[]>([]);
  const [allStudents, setAllStudents]                 = useState<AllStudent[]>([]);
  const [studentSearch, setStudentSearch]             = useState('');

  // Assign Instructor modal
  const [assigningSectionId, setAssigningSectionId]   = useState<string | null>(null);
  const [assignInstructorId, setAssignInstructorId]   = useState<string>('');

  // ── helpers ────────────────────────────────────────────────────────────────

  const getSlots = (): any[] => {
    try {
      const p = JSON.parse(scheduleJson);
      return Array.isArray(p) ? p : [];
    } catch { return []; }
  };

  const addSlot = () => {
    const periodObj = PERIODS.find(p => p.number === builderPeriod);
    if (!periodObj) return;
    const newSlot = { day: builderDay, startTime: periodObj.start, endTime: periodObj.end, room: builderRoom.trim() || undefined };
    const cur = getSlots();
    if (cur.some(s => s.day === newSlot.day && s.startTime === newSlot.startTime)) { alert('Slot already exists.'); return; }
    setScheduleJson(JSON.stringify([...cur, newSlot]));
  };

  const removeSlot = (index: number) =>
    setScheduleJson(JSON.stringify(getSlots().filter((_, i) => i !== index)));

  const formatSchedule = (json: string) => {
    try {
      const items = JSON.parse(json);
      if (Array.isArray(items))
        return items.map((it: any) => `${it.day ?? it.Day} ${it.startTime ?? it.StartTime}–${it.endTime ?? it.EndTime}${it.room ? ` (${it.room})` : ''}`).join(', ');
    } catch { return json || 'None'; }
    return 'None';
  };

  const getCourseInfo   = (id: string) => { const c = courses.find(x => x.id === id); return c ? `${c.code} - ${c.name}` : id; };
  const getInstructorName = (id: string) => instructors.find(x => x.id === id)?.fullName ?? id;

  // ── data loading ──────────────────────────────────────────────────────────

  const loadData = async () => {
    setLoading(true);
    try {
      const [secRes, courseRes, instRes, semRes] = await Promise.all([
        apiClient.get<Section[]>('/admin/sections'),
        apiClient.get<Course[]>('/courses'),
        apiClient.get<Instructor[]>('/admin/instructors'),
        apiClient.get<Semester[]>('/semesters'),
      ]);
      setSections(secRes.data);
      setCourses(courseRes.data);
      setInstructors(instRes.data);
      setSemesters(semRes.data);
      const cur = semRes.data.find(s => s.isCurrent) ?? semRes.data[0];
      if (cur) setSemesterId(cur.id);
    } catch {}
    finally { setLoading(false); }
  };

  useEffect(() => { loadData(); }, []);

  // ── edit / create form ────────────────────────────────────────────────────

  const openNewForm = () => {
    setIsNew(true);
    const defSem = semesters.find(s => s.isCurrent) ?? semesters[0];
    setEditingSection({ id: '', courseId: courses[0]?.id || '', instructorId: instructors[0]?.id || '', semesterId: defSem?.id ?? '', capacity: 30, scheduleJson: '[]' });
    setCourseId(courses[0]?.id || '');
    setInstructorId(instructors[0]?.id || '');
    if (defSem) setSemesterId(defSem.id);
    setCapacity(30);
    setScheduleJson('[]');
  };

  const openEditForm = (sec: Section) => {
    setIsNew(false);
    setEditingSection(sec);
    setCourseId(sec.courseId);
    setInstructorId(sec.instructorId);
    setSemesterId(sec.semesterId);
    setCapacity(sec.capacity);
    setScheduleJson(sec.scheduleJson);
  };

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingSection || !courseId) return;
    const payload = { id: editingSection.id, courseId, instructorId, semesterId, capacity, scheduleJson };
    const req = isNew ? apiClient.post('/admin/sections', payload) : apiClient.put(`/sections/${editingSection.id}`, payload);
    req.then(() => { alert(isNew ? 'Section created!' : 'Section updated!'); setEditingSection(null); loadData(); })
       .catch(err => alert(err.response?.data?.message || 'Failed to save.'));
  };

  const handleDelete = (id: string) => {
    if (!window.confirm('Soft-delete this section?')) return;
    apiClient.delete(`/sections/${id}`)
      .then(() => { alert('Section soft-deleted.'); loadData(); })
      .catch(err => alert(err.response?.data?.message || 'Failed.'));
  };

  // ── manage students ───────────────────────────────────────────────────────

  const openManageStudents = async (sec: Section) => {
    setManagingSectionId(sec.id);
    setManagingSectionName(getCourseInfo(sec.courseId));
    setStudentSearch('');
    try {
      const [enrolledRes, allRes] = await Promise.all([
        apiClient.get<StudentItem[]>(`/admin/sections/${sec.id}/students`),
        apiClient.get<AllStudent[]>('/admin/students'),
      ]);
      setEnrolledStudents(enrolledRes.data);
      setAllStudents(allRes.data);
    } catch {}
  };

  const handleEnroll = async (studentId: string) => {
    try {
      await apiClient.post(`/admin/sections/${managingSectionId}/enroll/${studentId}`, {});
      if (managingSectionId) {
        const res = await apiClient.get<StudentItem[]>(`/admin/sections/${managingSectionId}/students`);
        setEnrolledStudents(res.data);
      }
    } catch (err: any) { alert(err.response?.data?.message || 'Enroll failed.'); }
  };

  const handleUnenroll = async (studentId: string) => {
    if (!window.confirm('Remove this student?')) return;
    try {
      await apiClient.delete(`/admin/sections/${managingSectionId}/enroll/${studentId}`);
      setEnrolledStudents(prev => prev.filter(e => e.studentId !== studentId));
    } catch (err: any) { alert(err.response?.data?.message || 'Unenroll failed.'); }
  };

  // ── assign instructor ─────────────────────────────────────────────────────

  const openAssignInstructor = (sec: Section) => {
    setAssigningSectionId(sec.id);
    setAssignInstructorId(sec.instructorId);
  };

  const handleAssignInstructor = async () => {
    if (!assigningSectionId || !assignInstructorId) return;
    try {
      await apiClient.put(`/admin/sections/${assigningSectionId}/instructor/${assignInstructorId}`, {});
      alert('Instructor assigned!');
      setAssigningSectionId(null);
      loadData();
    } catch (err: any) { alert(err.response?.data?.message || 'Assign failed.'); }
  };

  // ── render ────────────────────────────────────────────────────────────────

  if (loading && sections.length === 0) return <div className="brand-subtitle">Loading sections...</div>;

  const enrolledIds = new Set(enrolledStudents.map(e => e.studentId));
  const filteredAll = allStudents.filter(s =>
    !enrolledIds.has(s.id) && (
      s.fullName.toLowerCase().includes(studentSearch.toLowerCase()) ||
      s.studentNumber.includes(studentSearch)
    )
  );

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      {/* Header */}
      <div className="glass-panel" style={{ padding: '32px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <h1 style={{ marginBottom: '8px' }}>Sections Management</h1>
          <p style={{ color: 'var(--text-muted)' }}>Manage sections, schedules, instructors, and student enrollment.</p>
        </div>
        <button className="glass-btn primary" onClick={openNewForm}>➕ Create Section</button>
      </div>

      {/* Sections table */}
      <div className="glass-panel" style={{ padding: '24px', overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
          <thead>
            <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)', fontSize: '0.9rem' }}>
              <th style={{ padding: '16px 12px' }}>Course</th>
              <th style={{ padding: '16px 12px' }}>Instructor</th>
              <th style={{ padding: '16px 12px' }}>Students</th>
              <th style={{ padding: '16px 12px' }}>Schedule</th>
              <th style={{ padding: '16px 12px' }}>Cap</th>
              <th style={{ padding: '16px 12px', textAlign: 'right' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {sections.map(sec => (
              <tr key={sec.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                <td style={{ padding: '16px 12px', fontWeight: 'bold' }}>{getCourseInfo(sec.courseId)}</td>
                <td style={{ padding: '16px 12px' }}>{getInstructorName(sec.instructorId)}</td>
                <td style={{ padding: '16px 12px' }}>{sec.studentCount ?? '—'}</td>
                <td style={{ padding: '16px 12px', fontSize: '0.85rem' }}>{formatSchedule(sec.scheduleJson)}</td>
                <td style={{ padding: '16px 12px' }}>{sec.capacity}</td>
                <td style={{ padding: '16px 12px', textAlign: 'right' }}>
                  <div style={{ display: 'flex', gap: '6px', justifyContent: 'flex-end', flexWrap: 'wrap' }}>
                    <button className="glass-btn" style={{ padding: '5px 10px', fontSize: '0.8rem' }} onClick={() => openManageStudents(sec)}>👥 Students</button>
                    <button className="glass-btn" style={{ padding: '5px 10px', fontSize: '0.8rem' }} onClick={() => openAssignInstructor(sec)}>👤 Instructor</button>
                    <button className="glass-btn" style={{ padding: '5px 10px', fontSize: '0.8rem' }} onClick={() => openEditForm(sec)}>✏️ Edit</button>
                    <button className="glass-btn" style={{ padding: '5px 10px', fontSize: '0.8rem', borderColor: 'var(--error)', color: 'var(--error)' }} onClick={() => handleDelete(sec.id)}>🗑️</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* ── Manage Students Panel ── */}
      {managingSectionId && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', zIndex: 1000, display: 'flex', justifyContent: 'center', alignItems: 'center', padding: '20px' }}>
          <div className="glass-panel" style={{ width: '100%', maxWidth: '800px', maxHeight: '85vh', overflowY: 'auto', padding: '32px', background: '#1e293b' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
              <h2>Manage Students — {managingSectionName}</h2>
              <button className="glass-btn" onClick={() => setManagingSectionId(null)}>✕ Close</button>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '24px' }}>
              {/* Enrolled */}
              <div>
                <h3 style={{ color: 'var(--accent)', marginBottom: '12px' }}>Enrolled ({enrolledStudents.length})</h3>
                {enrolledStudents.length === 0
                  ? <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>No students enrolled.</p>
                  : enrolledStudents.map(s => (
                    <div key={s.studentId} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 12px', marginBottom: '6px', background: 'rgba(255,255,255,0.04)', borderRadius: '6px' }}>
                      <div>
                        <div style={{ fontWeight: 'bold', fontSize: '0.9rem' }}>{s.fullName}</div>
                        <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)' }}>{s.studentNumber}</div>
                      </div>
                      <button className="glass-btn" style={{ padding: '4px 10px', fontSize: '0.8rem', borderColor: 'var(--error)', color: 'var(--error)' }} onClick={() => handleUnenroll(s.studentId)}>Remove</button>
                    </div>
                  ))
                }
              </div>

              {/* Available */}
              <div>
                <h3 style={{ color: 'var(--text-muted)', marginBottom: '12px' }}>Add Student</h3>
                <input
                  type="text"
                  className="form-input"
                  placeholder="Search name or number..."
                  style={{ marginBottom: '12px', width: '100%' }}
                  value={studentSearch}
                  onChange={e => setStudentSearch(e.target.value)}
                />
                {filteredAll.slice(0, 30).map(s => (
                  <div key={s.id} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', padding: '8px 12px', marginBottom: '6px', background: 'rgba(255,255,255,0.02)', borderRadius: '6px' }}>
                    <div>
                      <div style={{ fontWeight: 'bold', fontSize: '0.9rem' }}>{s.fullName}</div>
                      <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)' }}>{s.studentNumber}</div>
                    </div>
                    <button className="glass-btn primary" style={{ padding: '4px 10px', fontSize: '0.8rem' }} onClick={() => handleEnroll(s.id)}>Add</button>
                  </div>
                ))}
                {filteredAll.length === 0 && <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>No more students to add.</p>}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ── Assign Instructor Modal ── */}
      {assigningSectionId && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', zIndex: 1000, display: 'flex', justifyContent: 'center', alignItems: 'center', padding: '20px' }}>
          <div className="glass-panel" style={{ width: '100%', maxWidth: '400px', padding: '32px', background: '#1e293b' }}>
            <h2 style={{ marginBottom: '20px' }}>Assign Instructor</h2>
            <div className="form-group" style={{ marginBottom: '20px' }}>
              <label className="form-label">Instructor</label>
              <select className="form-input" style={{ background: 'rgba(15,23,42,0.9)' }} value={assignInstructorId} onChange={e => setAssignInstructorId(e.target.value)}>
                {instructors.map(i => <option key={i.id} value={i.id}>{i.fullName}</option>)}
              </select>
            </div>
            <div style={{ display: 'flex', gap: '12px' }}>
              <button className="glass-btn" style={{ flex: 1, justifyContent: 'center' }} onClick={() => setAssigningSectionId(null)}>Cancel</button>
              <button className="glass-btn primary" style={{ flex: 1, justifyContent: 'center' }} onClick={handleAssignInstructor}>Save</button>
            </div>
          </div>
        </div>
      )}

      {/* ── Create / Edit Section Modal ── */}
      {editingSection && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', zIndex: 1000, display: 'flex', justifyContent: 'center', alignItems: 'center', padding: '20px' }}>
          <div className="glass-panel" style={{ width: '100%', maxWidth: '500px', maxHeight: '90vh', overflowY: 'auto', padding: '32px', background: '#1e293b' }}>
            <h2 style={{ marginBottom: '16px' }}>{isNew ? 'Create Section' : 'Edit Section'}</h2>
            <form onSubmit={handleSave} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div className="form-group">
                <label className="form-label">Course</label>
                <select className="form-input" style={{ background: 'rgba(15,23,42,0.9)' }} required value={courseId} onChange={e => setCourseId(e.target.value)}>
                  {courses.map(c => <option key={c.id} value={c.id}>{c.code} - {c.name}</option>)}
                </select>
              </div>
              <div className="form-group">
                <label className="form-label">Instructor</label>
                <select className="form-input" style={{ background: 'rgba(15,23,42,0.9)' }} required value={instructorId} onChange={e => setInstructorId(e.target.value)}>
                  {instructors.map(i => <option key={i.id} value={i.id}>{i.fullName}</option>)}
                </select>
              </div>
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group">
                  <label className="form-label">Semester</label>
                  <select className="form-input" style={{ background: 'rgba(15,23,42,0.9)' }} required value={semesterId} onChange={e => setSemesterId(e.target.value)}>
                    {semesters.map(s => <option key={s.id} value={s.id}>{s.name}{s.isCurrent ? ' (Current)' : ''}</option>)}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">Capacity</label>
                  <input type="number" className="form-input" required value={capacity} onChange={e => setCapacity(Number(e.target.value))} />
                </div>
              </div>

              {/* Schedule Builder */}
              <div className="form-group" style={{ border: '1px solid var(--border-color)', padding: '16px', borderRadius: '8px', background: 'rgba(0,0,0,0.2)', display: 'flex', flexDirection: 'column', gap: '12px' }}>
                <label className="form-label" style={{ fontWeight: 'bold', color: 'var(--accent)' }}>Schedule Builder</label>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  {getSlots().length === 0
                    ? <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>No slots added yet.</span>
                    : getSlots().map((slot, idx) => (
                      <div key={idx} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'rgba(255,255,255,0.03)', padding: '8px 12px', borderRadius: '4px' }}>
                        <span style={{ fontSize: '0.9rem' }}><strong>{slot.day}</strong> {slot.startTime}–{slot.endTime}{slot.room ? ` (${slot.room})` : ''}</span>
                        <button type="button" className="glass-btn" style={{ padding: '2px 8px', fontSize: '0.8rem', borderColor: 'var(--error)', color: 'var(--error)' }} onClick={() => removeSlot(idx)}>Remove</button>
                      </div>
                    ))
                  }
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                  <div className="form-group">
                    <label className="form-label" style={{ fontSize: '0.8rem' }}>Day</label>
                    <select className="form-input" style={{ background: 'rgba(15,23,42,0.9)', height: '36px', padding: '0 8px' }} value={builderDay} onChange={e => setBuilderDay(e.target.value)}>
                      {['Sun','Mon','Tue','Wed','Thu','Fri','Sat'].map(d => <option key={d} value={d}>{d}</option>)}
                    </select>
                  </div>
                  <div className="form-group">
                    <label className="form-label" style={{ fontSize: '0.8rem' }}>Period</label>
                    <select className="form-input" style={{ background: 'rgba(15,23,42,0.9)', height: '36px', padding: '0 8px' }} value={builderPeriod} onChange={e => setBuilderPeriod(Number(e.target.value))}>
                      {PERIODS.map(p => <option key={p.number} value={p.number}>Period {p.number} — {p.start} to {p.end}</option>)}
                    </select>
                  </div>
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: '12px', alignItems: 'flex-end' }}>
                  <div className="form-group">
                    <label className="form-label" style={{ fontSize: '0.8rem' }}>Room (Optional)</label>
                    <input type="text" className="form-input" style={{ height: '36px', padding: '0 8px' }} placeholder="e.g. C201" value={builderRoom} onChange={e => setBuilderRoom(e.target.value)} />
                  </div>
                  <button type="button" className="glass-btn primary" style={{ height: '36px', justifyContent: 'center', padding: '0 16px' }} onClick={addSlot}>Add Slot</button>
                </div>
              </div>

              <div style={{ display: 'flex', gap: '16px', marginTop: '8px' }}>
                <button type="button" className="glass-btn" style={{ flex: 1, justifyContent: 'center' }} onClick={() => setEditingSection(null)}>Cancel</button>
                <button type="submit" className="glass-btn primary" style={{ flex: 1, justifyContent: 'center' }}>Save Section</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
