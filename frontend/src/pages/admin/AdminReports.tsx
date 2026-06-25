import React, { useEffect, useState } from 'react';
import apiClient from '../../api/apiClient';

interface CourseInfo {
  id: string;
  code: string;
  name: string;
}

interface Section {
  id: string;
  courseId: string;
  course?: CourseInfo;
  // Convenience aliases if API projects them directly
  courseCode?: string;
  courseName?: string;
}

interface Student {
  id: string;
  studentNumber: string;
  fullName: string;
}

export const AdminReports: React.FC = () => {
  const [sections, setSections] = useState<Section[]>([]);
  const [students, setStudents] = useState<Student[]>([]);
  const [loading, setLoading] = useState(true);

  // Selection state — IDs are GUIDs (string) from the API
  const [selectedSectionId, setSelectedSectionId] = useState<string | ''>('');
  const [sectionReportType, setSectionReportType] = useState<'attendance' | 'results'>('attendance');
  const [sectionFormat, setSectionFormat] = useState<'pdf' | 'xlsx'>('pdf');
  const [downloadingSection, setDownloadingSection] = useState(false);

  const [selectedStudentId, setSelectedStudentId] = useState<string | ''>('');
  const [downloadingTranscript, setDownloadingTranscript] = useState(false);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [secRes, studentRes] = await Promise.all([
          apiClient.get<Section[]>('/sections'),
          apiClient.get<Student[]>('/students')
        ]);
        setSections(secRes.data);
        setStudents(studentRes.data);
      } catch (err) {
        console.error('Failed to load lists for reports', err);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, []);

  const handleDownloadSectionReport = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedSectionId) return;

    setDownloadingSection(true);
    const url = `/sections/${selectedSectionId}/${sectionReportType}/export?format=${sectionFormat}`;
    const filename = `${sectionReportType}_section_${selectedSectionId}.${sectionFormat}`;

    apiClient.get(url, { responseType: 'blob' })
      .then((res) => {
        const blob = new Blob([res.data], { type: res.headers['content-type'] as string });
        const link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      })
      .catch((err) => {
        console.error(err);
        alert('Failed to generate export file.');
      })
      .finally(() => {
        setDownloadingSection(false);
      });
  };

  const handleDownloadTranscript = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedStudentId) return;

    setDownloadingTranscript(true);
    const url = `/students/${selectedStudentId}/transcript/export?format=pdf`;
    const filename = `transcript_student_${selectedStudentId}.pdf`;

    apiClient.get(url, { responseType: 'blob' })
      .then((res) => {
        const blob = new Blob([res.data], { type: res.headers['content-type'] as string });
        const link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      })
      .catch((err) => {
        console.error(err);
        alert('Failed to generate transcript PDF.');
      })
      .finally(() => {
        setDownloadingTranscript(false);
      });
  };

  if (loading) {
    return <div className="brand-subtitle">Loading report directories...</div>;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px' }}>
        <h1 style={{ marginBottom: '8px' }}>System Reports</h1>
        <p style={{ color: 'var(--text-muted)' }}>Generate official PDF summaries and Excel exports.</p>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '24px' }}>
        
        {/* Section Report Card */}
        <div className="glass-panel" style={{ padding: '24px' }}>
          <h3 style={{ marginBottom: '16px', color: 'var(--accent)' }}>📚 Section Logs Export</h3>
          <form onSubmit={handleDownloadSectionReport} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div className="form-group">
              <label className="form-label">Course Section</label>
              <select
                className="form-input"
                style={{ background: 'rgba(15,23,42,0.9)' }}
                required
                value={selectedSectionId}
                onChange={(e) => setSelectedSectionId(e.target.value)}
              >
                <option value="">-- Choose Section --</option>
                {sections.map(s => (
                  <option key={s.id} value={s.id}>
                    {s.courseCode ?? s.course?.code ?? s.courseId} - {s.courseName ?? s.course?.name ?? ''}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label className="form-label">Report Category</label>
              <select
                className="form-input"
                style={{ background: 'rgba(15,23,42,0.9)' }}
                value={sectionReportType}
                onChange={(e) => setSectionReportType(e.target.value as 'attendance' | 'results')}
              >
                <option value="attendance">Attendance Ledger</option>
                <option value="results">Academic Grading Sheets</option>
              </select>
            </div>

            <div className="form-group">
              <label className="form-label">Format Type</label>
              <select
                className="form-input"
                style={{ background: 'rgba(15,23,42,0.9)' }}
                value={sectionFormat}
                onChange={(e) => setSectionFormat(e.target.value as 'pdf' | 'xlsx')}
              >
                <option value="pdf">Adobe PDF Document (.pdf)</option>
                <option value="xlsx">Excel Spreadsheet (.xlsx)</option>
              </select>
            </div>

            <button
              type="submit"
              className="glass-btn primary"
              disabled={!selectedSectionId || downloadingSection}
              style={{ justifyContent: 'center', marginTop: '8px' }}
            >
              {downloadingSection ? 'Generating file...' : '📥 Download Report'}
            </button>
          </form>
        </div>

        {/* Student Transcript Card */}
        <div className="glass-panel" style={{ padding: '24px' }}>
          <h3 style={{ marginBottom: '16px', color: 'var(--accent)' }}>🎓 Student Transcript PDF</h3>
          <form onSubmit={handleDownloadTranscript} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div className="form-group">
              <label className="form-label">Select Student</label>
              <select
                className="form-input"
                style={{ background: 'rgba(15,23,42,0.9)' }}
                required
                value={selectedStudentId}
                onChange={(e) => setSelectedStudentId(e.target.value)}
              >
                <option value="">-- Choose Student --</option>
                {students.map(s => (
                  <option key={s.id} value={s.id}>{s.studentNumber} - {s.fullName}</option>
                ))}
              </select>
            </div>

            <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem' }}>
              Generates a formal transcript including overall GPA, total completed credit hours, and detailed course-by-course letter grades.
            </p>

            <button
              type="submit"
              className="glass-btn primary"
              disabled={!selectedStudentId || downloadingTranscript}
              style={{ justifyContent: 'center', marginTop: 'auto' }}
            >
              {downloadingTranscript ? 'Generating PDF...' : '📄 Download PDF Transcript'}
            </button>
          </form>
        </div>

      </div>
    </div>
  );
};
