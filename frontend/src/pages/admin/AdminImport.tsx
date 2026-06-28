import React, { useState } from 'react';
import apiClient from '../../api/apiClient';

interface RowError {
  rowNumber: number;
  studentNumber?: string;
  instructorId?: string;
  reason: string;
}

interface ImportSummary {
  totalRows: number;
  succeeded: number;
  failed: number;
  errors: RowError[];
}

export const AdminImport: React.FC = () => {
  const [file, setFile] = useState<File | null>(null);
  const [importType, setImportType] = useState<'students' | 'instructors'>('students');
  const [uploading, setUploading] = useState(false);
  const [summary, setSummary] = useState<ImportSummary | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0) {
      setFile(e.target.files[0]);
      setSummary(null);
      setErrorMessage(null);
    }
  };

  const handleUpload = (e: React.FormEvent) => {
    e.preventDefault();
    if (!file) return;

    setUploading(true);
    setSummary(null);
    setErrorMessage(null);

    const formData = new FormData();
    formData.append('file', file);

    const url = importType === 'students' 
      ? '/students/bulk-import' 
      : '/instructors/bulk-import';

    apiClient.post<ImportSummary>(url, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    })
      .then((res) => {
        setSummary(res.data);
        setFile(null);
      })
      .catch((err) => {
        console.error(err);
        setErrorMessage(err.response?.data?.message || 'File upload failed. Please ensure the file formatting is correct.');
      })
      .finally(() => {
        setUploading(false);
      });
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px' }}>
        <h1 style={{ marginBottom: '8px' }}>Bulk Account Import</h1>
        <p style={{ color: 'var(--text-muted)' }}>Upload CSV or Excel spreadsheets to provision student and instructor accounts.</p>
      </div>

      <div className="glass-panel" style={{ padding: '24px' }}>
        <form onSubmit={handleUpload} style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          <div className="form-group">
            <label className="form-label">Import Account Type</label>
            <div style={{ display: 'flex', gap: '16px' }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', cursor: 'pointer' }}>
                <input
                  type="radio"
                  name="importType"
                  checked={importType === 'students'}
                  onChange={() => setImportType('students')}
                />
                Students Directory
              </label>
              <label style={{ display: 'flex', alignItems: 'center', gap: '8px', cursor: 'pointer' }}>
                <input
                  type="radio"
                  name="importType"
                  checked={importType === 'instructors'}
                  onChange={() => setImportType('instructors')}
                />
                Instructors Directory
              </label>
            </div>
          </div>

          <div className="form-group" style={{ border: '2px dashed var(--border-color)', padding: '32px', borderRadius: 'var(--border-radius)', textAlign: 'center', cursor: 'pointer' }}>
            <label htmlFor="file-input" style={{ cursor: 'pointer', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px' }}>
              <span style={{ fontSize: '2.5rem' }}>📁</span>
              {file ? (
                <span style={{ fontWeight: 'bold', color: 'var(--accent)' }}>{file.name} ({(file.size / 1024).toFixed(1)} KB)</span>
              ) : (
                <>
                  <span>Drag & drop files or click to browse</span>
                  <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Supports CSV, XLS, XLSX formats</span>
                </>
              )}
            </label>
            <input
              id="file-input"
              type="file"
              accept=".csv,.xls,.xlsx"
              style={{ display: 'none' }}
              onChange={handleFileChange}
            />
          </div>

          <button
            type="submit"
            className="glass-btn primary"
            disabled={!file || uploading}
            style={{ alignSelf: 'flex-start', minWidth: '180px', justifyContent: 'center' }}
          >
            {uploading ? 'Processing Sheets...' : '📤 Start Import'}
          </button>
        </form>
      </div>

      {errorMessage && (
        <div style={{
          padding: '16px',
          background: 'rgba(239, 68, 68, 0.15)',
          border: '1px solid var(--error)',
          color: 'var(--error)',
          borderRadius: '8px',
          fontWeight: 500,
        }}>
          {errorMessage}
        </div>
      )}

      {summary && (
        <div className="glass-panel" style={{ padding: '24px' }}>
          <h3 style={{ marginBottom: '16px', color: 'var(--accent)' }}>📊 Import Execution Summary</h3>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '16px', marginBottom: '24px' }}>
            <div style={{ padding: '16px', background: 'rgba(255,255,255,0.02)', borderRadius: '8px', textAlign: 'center' }}>
              <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Total Rows</div>
              <div style={{ fontSize: '1.8rem', fontWeight: 'bold' }}>{summary.totalRows}</div>
            </div>
            <div style={{ padding: '16px', background: 'rgba(16, 185, 129, 0.05)', border: '1px solid rgba(16, 185, 129, 0.2)', borderRadius: '8px', textAlign: 'center', color: 'var(--success)' }}>
              <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Succeeded</div>
              <div style={{ fontSize: '1.8rem', fontWeight: 'bold' }}>{summary.succeeded}</div>
            </div>
            <div style={{ padding: '16px', background: 'rgba(239, 68, 68, 0.05)', border: '1px solid rgba(239, 68, 68, 0.2)', borderRadius: '8px', textAlign: 'center', color: 'var(--error)' }}>
              <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>Failed</div>
              <div style={{ fontSize: '1.8rem', fontWeight: 'bold' }}>{summary.failed}</div>
            </div>
          </div>

          {summary.errors.length > 0 && (
            <div>
              <h4 style={{ color: 'var(--error)', marginBottom: '12px' }}>⚠️ Row Processing Exceptions</h4>
              <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '300px', overflowY: 'auto', paddingRight: '8px' }}>
                {summary.errors.map((err, i) => (
                  <div key={i} style={{
                    padding: '12px 16px',
                    background: 'rgba(239, 68, 68, 0.05)',
                    border: '1px solid rgba(239, 68, 68, 0.1)',
                    borderRadius: '6px',
                    display: 'flex',
                    justifyContent: 'space-between',
                    fontSize: '0.9rem',
                    flexWrap: 'wrap',
                    gap: '12px'
                  }}>
                    <div>
                      <strong>Row {err.rowNumber}</strong>
                      {err.studentNumber && <span style={{ marginLeft: '12px', color: 'var(--text-muted)' }}>ID: {err.studentNumber}</span>}
                      {err.instructorId && <span style={{ marginLeft: '12px', color: 'var(--text-muted)' }}>ID: {err.instructorId}</span>}
                    </div>
                    <div style={{ color: 'var(--error)' }}>{err.reason}</div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
