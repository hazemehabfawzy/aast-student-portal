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
}

interface Course {
  id: string;
  code: string;
  name: string;
}

interface Instructor {
  id: string;
  fullName: string;
}

interface Semester {
  id: string;
  name: string;
  isCurrent: boolean;
}

export const AdminSections: React.FC = () => {
  const [sections, setSections] = useState<Section[]>([]);
  const [courses, setCourses] = useState<Course[]>([]);
  const [instructors, setInstructors] = useState<Instructor[]>([]);
  const [semesters, setSemesters] = useState<Semester[]>([]);
  const [loading, setLoading] = useState(true);

  // Form State
  const [editingSection, setEditingSection] = useState<Section | null>(null);
  const [isNew, setIsNew] = useState(false);
  const [courseId, setCourseId] = useState<string | ''>('');
  const [instructorId, setInstructorId] = useState<string | ''>('');
  const [semesterId, setSemesterId] = useState<string>('');
  const [capacity, setCapacity] = useState<number>(30);
  const [scheduleJson, setScheduleJson] = useState('[]');

  // Schedule Builder State
  const [builderDay, setBuilderDay] = useState('Sun');
  const [builderPeriod, setBuilderPeriod] = useState(1);
  const [builderRoom, setBuilderRoom] = useState('');

  const getSlots = (): any[] => {
    try {
      const parsed = JSON.parse(scheduleJson);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  };

  const addSlot = () => {
    const periodObj = PERIODS.find(p => p.number === builderPeriod);
    if (!periodObj) return;
    const newSlot = {
      day: builderDay,
      startTime: periodObj.start,
      endTime: periodObj.end,
      room: builderRoom.trim() || undefined,
    };
    const currentSlots = getSlots();
    if (currentSlots.some(s => s.day === newSlot.day && s.startTime === newSlot.startTime && s.room === newSlot.room)) {
      alert('This slot already exists.');
      return;
    }
    const updated = [...currentSlots, newSlot];
    setScheduleJson(JSON.stringify(updated));
  };

  const removeSlot = (index: number) => {
    const currentSlots = getSlots();
    const updated = currentSlots.filter((_, i) => i !== index);
    setScheduleJson(JSON.stringify(updated));
  };

  const loadData = async () => {
    setLoading(true);
    try {
      const [secRes, courseRes, instRes, semRes] = await Promise.all([
        apiClient.get<Section[]>('/sections'),
        apiClient.get<Course[]>('/courses'),
        apiClient.get<Instructor[]>('/instructors'),
        apiClient.get<Semester[]>('/semesters'),
      ]);
      setSections(secRes.data);
      setCourses(courseRes.data);
      setInstructors(instRes.data);
      setSemesters(semRes.data);
      // Default to current semester
      const current = semRes.data.find(s => s.isCurrent) ?? semRes.data[0];
      if (current) setSemesterId(current.id);
    } catch (err) {
      console.error('Failed to load sections data', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const openNewForm = () => {
    setIsNew(true);
    setEditingSection({
      id: '',
      courseId: courses[0]?.id || '',
      instructorId: instructors[0]?.id || '',
      semesterId: semesters.find(s => s.isCurrent)?.id ?? semesters[0]?.id ?? '',
      capacity: 30,
      scheduleJson: '[]'
    });
    setCourseId(courses[0]?.id || '');
    setInstructorId(instructors[0]?.id || '');
    const defaultSem = semesters.find(s => s.isCurrent) ?? semesters[0];
    if (defaultSem) setSemesterId(defaultSem.id);
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
    if (!editingSection || courseId === '' || instructorId === '') return;

    const payload = {
      id: editingSection.id,
      courseId,
      instructorId,
      semesterId,
      capacity,
      scheduleJson,
    };

    const request = isNew
      ? apiClient.post('/sections', payload)
      : apiClient.put(`/sections/${editingSection.id}`, payload);

    request
      .then(() => {
        alert(isNew ? 'Section created successfully!' : 'Section updated successfully!');
        setEditingSection(null);
        loadData();
      })
      .catch((err) => {
        alert(err.response?.data?.message || 'Failed to save section.');
      });
  };

  const handleDelete = (id: string) => {
    if (!window.confirm('Are you sure you want to soft-delete this section? Historical records will remain intact.')) return;
    apiClient.delete(`/sections/${id}`)
      .then(() => {
        alert('Section soft-deleted successfully.');
        loadData();
      })
      .catch((err) => {
        alert(err.response?.data?.message || 'Failed to soft-delete section.');
      });
  };

  const getCourseInfo = (id: string) => {
    const course = courses.find(c => c.id === id);
    return course ? `${course.code} - ${course.name}` : `Course ${id}`;
  };

  const getInstructorName = (id: string) => {
    const inst = instructors.find(i => i.id === id);
    return inst ? inst.fullName : `Instructor ${id}`;
  };

  const formatSchedule = (json: string) => {
    try {
      const items = JSON.parse(json);
      if (Array.isArray(items)) {
        const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
        return items.map((it: any) => {
          const dayVal = it.day ?? it.Day;
          const day = typeof dayVal === 'string'
            ? dayVal
            : (days[it.dayOfWeek ?? it.DayOfWeek] || 'Day');
          const start = it.startTime ?? it.StartTime ?? '';
          const end = it.endTime ?? it.EndTime ?? '';
          const room = it.room ?? it.Room;
          return `${day} ${start}–${end}${room ? ` (${room})` : ''}`;
        }).join(', ');
      }
    } catch {
      return json || 'None';
    }
    return 'None';
  };

  if (loading && sections.length === 0) {
    return <div className="brand-subtitle">Loading section logs...</div>;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '16px' }}>
        <div>
          <h1 style={{ marginBottom: '8px' }}>Sections Management</h1>
          <p style={{ color: 'var(--text-muted)' }}>Configure schedule lists, capacities, and teacher allocations.</p>
        </div>
        <button className="glass-btn primary" onClick={openNewForm}>
          ➕ Create Section
        </button>
      </div>

      <div className="glass-panel" style={{ padding: '24px', overflowX: 'auto' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
          <thead>
            <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)', fontSize: '0.9rem' }}>
              <th style={{ padding: '16px 12px' }}>ID</th>
              <th style={{ padding: '16px 12px' }}>Course</th>
              <th style={{ padding: '16px 12px' }}>Instructor</th>
              <th style={{ padding: '16px 12px' }}>Schedule</th>
              <th style={{ padding: '16px 12px' }}>Capacity</th>
              <th style={{ padding: '16px 12px', textAlign: 'right' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {sections.map((sec) => (
              <tr key={sec.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                <td style={{ padding: '16px 12px' }}>{sec.id}</td>
                <td style={{ padding: '16px 12px', fontWeight: 'bold' }}>{getCourseInfo(sec.courseId)}</td>
                <td style={{ padding: '16px 12px' }}>{getInstructorName(sec.instructorId)}</td>
                <td style={{ padding: '16px 12px', fontSize: '0.9rem' }}>{formatSchedule(sec.scheduleJson)}</td>
                <td style={{ padding: '16px 12px' }}>{sec.capacity} Seats</td>
                <td style={{ padding: '16px 12px', textAlign: 'right' }}>
                  <div style={{ display: 'flex', gap: '8px', justifyContent: 'flex-end' }}>
                    <button className="glass-btn" style={{ padding: '6px 12px', fontSize: '0.85rem' }} onClick={() => openEditForm(sec)}>
                      ✏️ Edit
                    </button>
                    <button className="glass-btn" style={{ padding: '6px 12px', fontSize: '0.85rem', borderColor: 'var(--error)', color: 'var(--error)' }} onClick={() => handleDelete(sec.id)}>
                      🗑️ Delete
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {editingSection && (
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
            <h2 style={{ marginBottom: '16px' }}>{isNew ? 'Create Section' : 'Edit Section'}</h2>
            <form onSubmit={handleSave} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
              <div className="form-group">
                <label className="form-label">Course</label>
                <select
                  className="form-input"
                  style={{ background: 'rgba(15,23,42,0.9)' }}
                  required
                  value={courseId}
                  onChange={(e) => setCourseId(e.target.value)}
                >
                  {courses.map(c => <option key={c.id} value={c.id}>{c.code} - {c.name}</option>)}
                </select>
              </div>

              <div className="form-group">
                <label className="form-label">Instructor</label>
                <select
                  className="form-input"
                  style={{ background: 'rgba(15,23,42,0.9)' }}
                  required
                  value={instructorId}
                  onChange={(e) => setInstructorId(e.target.value)}
                >
                  {instructors.map(i => <option key={i.id} value={i.id}>{i.fullName}</option>)}
                </select>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
                <div className="form-group">
                  <label className="form-label">Semester</label>
                  <select
                    className="form-input"
                    style={{ background: 'rgba(15,23,42,0.9)' }}
                    required
                    value={semesterId}
                    onChange={(e) => setSemesterId(e.target.value)}
                  >
                    {semesters.map(s => (
                      <option key={s.id} value={s.id}>
                        {s.name}{s.isCurrent ? ' (Current)' : ''}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="form-group">
                  <label className="form-label">Capacity</label>
                  <input
                    type="number"
                    className="form-input"
                    required
                    value={capacity}
                    onChange={(e) => setCapacity(Number(e.target.value))}
                  />
                </div>
              </div>

              <div className="form-group" style={{ border: '1px solid var(--border-color)', padding: '16px', borderRadius: '8px', background: 'rgba(0,0,0,0.2)', display: 'flex', flexDirection: 'column', gap: '12px' }}>
                <label className="form-label" style={{ fontWeight: 'bold', color: 'var(--accent)', marginBottom: '4px' }}>Schedule Builder</label>
                
                {/* Current Slots List */}
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginBottom: '8px' }}>
                  {getSlots().length === 0 ? (
                    <span style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>No schedule slots added yet.</span>
                  ) : (
                    getSlots().map((slot, idx) => (
                      <div key={idx} style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', background: 'rgba(255,255,255,0.03)', padding: '8px 12px', borderRadius: '4px', border: '1px solid rgba(255,255,255,0.05)' }}>
                        <span style={{ fontSize: '0.9rem' }}>
                          <strong>{slot.day ?? slot.Day}</strong> {slot.startTime ?? slot.StartTime} – {slot.endTime ?? slot.EndTime} {slot.room || slot.Room ? `(${slot.room ?? slot.Room})` : ''}
                        </span>
                        <button
                          type="button"
                          className="glass-btn"
                          style={{ padding: '2px 8px', fontSize: '0.8rem', borderColor: 'var(--error)', color: 'var(--error)', background: 'transparent' }}
                          onClick={() => removeSlot(idx)}
                        >
                          Remove
                        </button>
                      </div>
                    ))
                  )}
                </div>

                {/* Add Slot Controls */}
                <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                    <div className="form-group">
                      <label className="form-label" style={{ fontSize: '0.8rem' }}>Day</label>
                      <select
                        className="form-input"
                        style={{ background: 'rgba(15,23,42,0.9)', height: '36px', padding: '0 8px' }}
                        value={builderDay}
                        onChange={(e) => setBuilderDay(e.target.value)}
                      >
                        {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map(d => <option key={d} value={d}>{d}</option>)}
                      </select>
                    </div>

                    <div className="form-group">
                      <label className="form-label" style={{ fontSize: '0.8rem' }}>Period</label>
                      <select
                        className="form-input"
                        style={{ background: 'rgba(15,23,42,0.9)', height: '36px', padding: '0 8px' }}
                        value={builderPeriod}
                        onChange={(e) => setBuilderPeriod(Number(e.target.value))}
                      >
                        {PERIODS.map(p => (
                          <option key={p.number} value={p.number}>
                            Period {p.number} — {p.start} to {p.end}
                          </option>
                        ))}
                      </select>
                    </div>
                  </div>

                  <div style={{ display: 'grid', gridTemplateColumns: '1fr auto', gap: '12px', alignItems: 'flex-end' }}>
                    <div className="form-group">
                      <label className="form-label" style={{ fontSize: '0.8rem' }}>Room (Optional)</label>
                      <input
                        type="text"
                        className="form-input"
                        style={{ height: '36px', padding: '0 8px' }}
                        placeholder="e.g. C201"
                        value={builderRoom}
                        onChange={(e) => setBuilderRoom(e.target.value)}
                      />
                    </div>
                    <button
                      type="button"
                      className="glass-btn primary"
                      style={{ height: '36px', justifyContent: 'center', padding: '0 16px' }}
                      onClick={addSlot}
                    >
                      Add Slot
                    </button>
                  </div>
                </div>
              </div>

              <div className="form-group">
                <label className="form-label">Raw Schedule JSON (Auto-generated)</label>
                <textarea
                  className="form-input"
                  style={{ minHeight: '60px', fontFamily: 'monospace', background: 'rgba(0,0,0,0.1)' }}
                  required
                  value={scheduleJson}
                  onChange={(e) => setScheduleJson(e.target.value)}
                />
              </div>

              <div style={{ display: 'flex', gap: '16px', marginTop: '16px' }}>
                <button
                  type="button"
                  className="glass-btn"
                  style={{ flex: 1, justifyContent: 'center' }}
                  onClick={() => setEditingSection(null)}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="glass-btn primary"
                  style={{ flex: 1, justifyContent: 'center' }}
                >
                  Save Section
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
