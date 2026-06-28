import React, { useEffect, useState } from 'react';
import apiClient from '../../api/apiClient';

interface GradingPolicy {
  id: string;     // GUID
  courseId: string | null;
  week7Weight: number;
  week12Weight: number;
  prefinalWeight: number;
  finalWeight: number;
}

interface GradeScale {
  id: string;       // GUID
  letter: string;   // e.g. "A+", "B"
  minPercent: number | null;
  maxPercent: number | null;
  gpaPoints: number | null;
  countsTowardGpa: boolean;
}

export const AdminPolicies: React.FC = () => {
  const [scales, setScales] = useState<GradeScale[]>([]);
  const [loading, setLoading] = useState(true);

  // Policy Form Editing (We take the first one or allow editing)
  const [policyId, setPolicyId] = useState<string>('');
  const [w7, setW7] = useState('0.2');
  const [w12, setW12] = useState('0.2');
  const [prefinal, setPrefinal] = useState('0.2');
  const [final, setFinal] = useState('0.4');

  // Scale editing state
  const [selectedScaleId, setSelectedScaleId] = useState<string | ''>('');
  const [letterGrade, setLetterGrade] = useState('');
  const [minScore, setMinScore] = useState<number>(60);

  const loadData = async () => {
    setLoading(true);
    try {
      const [policyRes, scaleRes] = await Promise.all([
        apiClient.get<GradingPolicy[]>('/grading-policy'),
        apiClient.get<GradeScale[]>('/grade-scale'),
      ]);
      setScales(scaleRes.data);
      if (policyRes.data.length > 0) {
        const p = policyRes.data[0];
        setPolicyId(p.id);
        setW7(p.week7Weight.toString());
        setW12(p.week12Weight.toString());
        setPrefinal(p.prefinalWeight.toString());
        setFinal(p.finalWeight.toString());
      }
    } catch (err) {
      console.error('Failed to load policy scales', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleSavePolicy = (e: React.FormEvent) => {
    e.preventDefault();
    const sum = parseFloat(w7) + parseFloat(w12) + parseFloat(prefinal) + parseFloat(final);
    if (Math.abs(sum - 1.0) > 0.001) {
      alert(`Weights must sum to 1.0 (Current sum: ${sum.toFixed(3)})`);
      return;
    }

    const payload = {
      id: policyId,
      week7Weight: parseFloat(w7),
      week12Weight: parseFloat(w12),
      prefinalWeight: parseFloat(prefinal),
      finalWeight: parseFloat(final),
    };

    apiClient.put(`/grading-policy/${policyId}`, payload)
      .then(() => {
        alert('Grading policy weights saved successfully.');
        loadData();
      })
      .catch((err) => {
        alert(err.response?.data?.message || 'Failed to save grading policy.');
      });
  };

  const handleEditScale = (scale: GradeScale) => {
    setSelectedScaleId(scale.id);
    setLetterGrade(scale.letter);
    setMinScore(scale.minPercent ?? 0);
  };

  const handleSaveScale = (e: React.FormEvent) => {
    e.preventDefault();
    if (selectedScaleId === '') return;

    const payload = {
      id: selectedScaleId,
      letterGrade,
      minScore,
    };

    apiClient.put(`/grade-scale/${selectedScaleId}`, payload)
      .then(() => {
        alert('Grade scale threshold saved successfully.');
        setSelectedScaleId('');
        loadData();
      })
      .catch((err) => {
        alert(err.response?.data?.message || 'Failed to save grade scale.');
      });
  };

  if (loading) {
    return <div className="brand-subtitle">Loading policies and grade scales...</div>;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px' }}>
        <h1 style={{ marginBottom: '8px' }}>Grading Policies & Scales</h1>
        <p style={{ color: 'var(--text-muted)' }}>Configure academic assessment splits and score letter grade cut-offs.</p>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))', gap: '24px' }}>
        <div className="glass-panel" style={{ padding: '24px' }}>
          <h3 style={{ marginBottom: '16px', color: 'var(--accent)' }}>⚖️ Assessment Weight Split</h3>
          <form onSubmit={handleSavePolicy} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div className="form-group">
              <label className="form-label">Week 7 Exam (Weight)</label>
              <input
                type="number"
                step="0.01"
                min={0}
                max={1}
                className="form-input"
                required
                value={w7}
                onChange={(e) => setW7(e.target.value)}
              />
            </div>
            <div className="form-group">
              <label className="form-label">Week 12 Exam (Weight)</label>
              <input
                type="number"
                step="0.01"
                min={0}
                max={1}
                className="form-input"
                required
                value={w12}
                onChange={(e) => setW12(e.target.value)}
              />
            </div>
            <div className="form-group">
              <label className="form-label">Prefinal Classwork (Weight)</label>
              <input
                type="number"
                step="0.01"
                min={0}
                max={1}
                className="form-input"
                required
                value={prefinal}
                onChange={(e) => setPrefinal(e.target.value)}
              />
            </div>
            <div className="form-group">
              <label className="form-label">Final Exam (Weight)</label>
              <input
                type="number"
                step="0.01"
                min={0}
                max={1}
                className="form-input"
                required
                value={final}
                onChange={(e) => setFinal(e.target.value)}
              />
            </div>
            <div style={{ fontSize: '0.85rem', color: 'var(--text-muted)' }}>
              Total Sum: <strong style={{ color: Math.abs((parseFloat(w7) || 0) + (parseFloat(w12) || 0) + (parseFloat(prefinal) || 0) + (parseFloat(final) || 0) - 1.0) < 0.001 ? 'var(--success)' : 'var(--error)' }}>
                {((parseFloat(w7) || 0) + (parseFloat(w12) || 0) + (parseFloat(prefinal) || 0) + (parseFloat(final) || 0)).toFixed(3)}
              </strong> (must equal 1.000)
            </div>
            <button type="submit" className="glass-btn primary" style={{ justifyContent: 'center', marginTop: '8px' }}>
              Save Weight Splits
            </button>
          </form>
        </div>

        <div className="glass-panel" style={{ padding: '24px' }}>
          <h3 style={{ marginBottom: '16px', color: 'var(--accent)' }}>📊 Grade Scale Thresholds</h3>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', marginBottom: '20px' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-color)', color: 'var(--text-muted)', fontSize: '0.85rem' }}>
                <th style={{ padding: '8px' }}>Grade</th>
                <th style={{ padding: '8px' }}>Min Score (%)</th>
                <th style={{ padding: '8px', textAlign: 'right' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {scales.map((s) => (
                <tr key={s.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                  <td style={{ padding: '8px', fontWeight: 'bold' }}>{s.letter}</td>
                  <td style={{ padding: '8px' }}>{s.minPercent ?? '-'} %</td>
                  <td style={{ padding: '8px', textAlign: 'right' }}>
                    <button className="glass-btn" style={{ padding: '4px 8px', fontSize: '0.75rem' }} onClick={() => handleEditScale(s)}>
                      ✏️ Edit
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {selectedScaleId !== '' && (
            <div style={{ padding: '16px', background: 'rgba(255,255,255,0.02)', borderRadius: '8px', border: '1px solid var(--border-color)' }}>
              <h4 style={{ marginBottom: '12px' }}>Edit Grade: {letterGrade}</h4>
              <form onSubmit={handleSaveScale} style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                <div className="form-group" style={{ margin: 0 }}>
                  <label className="form-label">Minimum Score (%)</label>
                  <input
                    type="number"
                    min={0}
                    max={100}
                    className="form-input"
                    required
                    value={minScore}
                    onChange={(e) => setMinScore(Number(e.target.value))}
                  />
                </div>
                <div style={{ display: 'flex', gap: '8px' }}>
                  <button type="button" className="glass-btn" style={{ flex: 1, padding: '6px', justifyContent: 'center' }} onClick={() => setSelectedScaleId('')}>
                    Cancel
                  </button>
                  <button type="submit" className="glass-btn primary" style={{ flex: 1, padding: '6px', justifyContent: 'center' }}>
                    Save Threshold
                  </button>
                </div>
              </form>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
