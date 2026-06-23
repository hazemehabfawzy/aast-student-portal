import React, { useEffect, useState } from 'react';
import apiClient from '../../api/apiClient';

interface Section {
  sectionId: number;
  courseCode: string;
  courseName: string;
  instructorName: string;
  scheduleJson: string;
  isEnrolled: boolean;
}

interface ParsedSchedule {
  dayOfWeek: number; // 0 = Sunday, 1 = Monday, etc.
  startTime: string;
  endTime: string;
  courseCode: string;
  courseName: string;
  instructorName: string;
}

const DAYS = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

export const StudentSchedule: React.FC = () => {
  const [scheduleItems, setScheduleItems] = useState<ParsedSchedule[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Semester 1 is the seeded semester
    apiClient.get<Section[]>('/sections?semesterId=1')
      .then((res) => {
        const dayMap: { [key: string]: number } = {
          'Sun': 0, 'Sunday': 0,
          'Mon': 1, 'Monday': 1,
          'Tue': 2, 'Tuesday': 2,
          'Wed': 3, 'Wednesday': 3,
          'Thu': 4, 'Thursday': 4,
          'Fri': 5, 'Friday': 5,
          'Sat': 6, 'Saturday': 6
        };

        const parseTimeToMinutes = (timeStr: string): number => {
          try {
            const match = timeStr.match(/^(\d+):(\d+)\s*(AM|PM)$/i);
            if (!match) return 0;
            let hours = parseInt(match[1], 10);
            const minutes = parseInt(match[2], 10);
            const ampm = match[3].toUpperCase();
            if (ampm === 'PM' && hours < 12) hours += 12;
            if (ampm === 'AM' && hours === 12) hours = 0;
            return hours * 60 + minutes;
          } catch {
            return 0;
          }
        };

        const enrolledSections = res.data.filter(s => s.isEnrolled);
        const items: ParsedSchedule[] = [];

        enrolledSections.forEach((sec) => {
          try {
            const rawItems = JSON.parse(sec.scheduleJson);
            if (Array.isArray(rawItems)) {
              rawItems.forEach((raw) => {
                const dayVal = raw.day ?? raw.Day;
                let parsedDayOfWeek = 0;
                if (typeof raw.dayOfWeek === 'number') {
                  parsedDayOfWeek = raw.dayOfWeek;
                } else if (typeof raw.DayOfWeek === 'number') {
                  parsedDayOfWeek = raw.DayOfWeek;
                } else if (typeof dayVal === 'string') {
                  parsedDayOfWeek = dayMap[dayVal] ?? 0;
                }

                items.push({
                  dayOfWeek: parsedDayOfWeek,
                  startTime: raw.startTime ?? raw.StartTime,
                  endTime: raw.endTime ?? raw.EndTime,
                  courseCode: sec.courseCode,
                  courseName: sec.courseName,
                  instructorName: sec.instructorName
                });
              });
            }
          } catch (e) {
            console.error('Failed to parse schedule JSON for section', sec.sectionId, e);
          }
        });

        // Sort items by day and start time
        items.sort((a, b) => {
          if (a.dayOfWeek !== b.dayOfWeek) return a.dayOfWeek - b.dayOfWeek;
          return parseTimeToMinutes(a.startTime) - parseTimeToMinutes(b.startTime);
        });

        setScheduleItems(items);
      })
      .catch((err) => {
        console.error(err);
        setError('Unable to load class schedule. Please make sure registration is open and you are enrolled.');
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  if (loading) {
    return <div className="brand-subtitle">Loading schedule...</div>;
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div className="glass-panel" style={{ padding: '32px' }}>
        <h1 style={{ marginBottom: '8px' }}>Weekly Schedule</h1>
        <p style={{ color: 'var(--text-muted)' }}>Your weekly class timetable and lecture schedules.</p>
      </div>

      {error ? (
        <div className="glass-panel" style={{ padding: '24px', color: 'var(--text-muted)', textAlign: 'center' }}>
          {error}
        </div>
      ) : scheduleItems.length === 0 ? (
        <div className="glass-panel" style={{ padding: '40px', textAlign: 'center' }}>
          <p style={{ color: 'var(--text-muted)' }}>You have no enrolled classes scheduled for this semester.</p>
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: '24px' }}>
          {DAYS.map((dayName, dayIndex) => {
            const dayLectures = scheduleItems.filter(item => item.dayOfWeek === dayIndex);
            if (dayLectures.length === 0) return null;

            return (
              <div key={dayName} className="glass-panel" style={{ padding: '24px' }}>
                <h3 style={{ borderBottom: '1px solid var(--border-color)', paddingBottom: '12px', marginBottom: '16px', color: 'var(--accent)' }}>
                  {dayName}
                </h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                  {dayLectures.map((lecture, idx) => (
                    <div key={idx} style={{
                      padding: '16px',
                      background: 'rgba(255,255,255,0.03)',
                      borderLeft: '4px solid var(--primary)',
                      borderRadius: '4px'
                    }}>
                      <div style={{ fontWeight: 'bold', fontSize: '1.05rem' }}>{lecture.courseCode}</div>
                      <div style={{ fontSize: '0.9rem', color: 'var(--text-main)', margin: '4px 0' }}>{lecture.courseName}</div>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>👤 {lecture.instructorName}</div>
                      <div style={{ fontSize: '0.8rem', color: 'var(--accent)', marginTop: '8px', fontWeight: 'bold' }}>
                        ⏱️ {lecture.startTime} - {lecture.endTime}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
