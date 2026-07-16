import React, { useCallback, useEffect, useRef, useState } from 'react';
import apiClient from '../api/apiClient';

interface ChatMessage {
  id: string;
  senderName: string;
  senderRole: string;
  message: string;
  sentAt: string;
  isRead: boolean;
}

interface SectionItem {
  id: string;
  courseCode: string;
  courseName: string;
  instructorName?: string;
}

interface ChatPageProps {
  role: 'student' | 'instructor';
}

export const ChatPage: React.FC<ChatPageProps> = ({ role }) => {
  const [sections, setSections] = useState<SectionItem[]>([]);
  const [selectedSectionId, setSelectedSectionId] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [loadingSections, setLoadingSections] = useState(true);
  const [loadingMessages, setLoadingMessages] = useState(false);
  const [sending, setSending] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const selectedSection = sections.find((s) => s.id === selectedSectionId);

  const loadSections = useCallback(() => {
    setLoadingSections(true);
    const endpoint = role === 'student'
      ? '/students/me/schedule'
      : '/instructor/sections';

    apiClient.get(endpoint)
      .then((res: { data: Record<string, unknown>[] }) => {
        const data = res.data as Record<string, unknown>[];
        const mapped: SectionItem[] = data.map((item) => ({
          id: String(item.sectionId ?? item.id ?? item.Id),
          courseCode: String(item.courseCode ?? item.CourseCode ?? ''),
          courseName: String(item.courseName ?? item.CourseName ?? ''),
          instructorName: item.instructorName
            ? String(item.instructorName)
            : undefined,
        }));
        setSections(mapped);
        if (mapped.length > 0 && !selectedSectionId) {
          setSelectedSectionId(mapped[0].id);
        }
      })
      .catch(() => setSections([]))
      .finally(() => setLoadingSections(false));
  }, [role, selectedSectionId]);

  const loadMessages = useCallback((sectionId: string) => {
    setLoadingMessages(true);
    apiClient.get<ChatMessage[]>(`/chat/sections/${sectionId}`)
      .then((res: { data: ChatMessage[] }) => {
        setMessages(res.data.map((m: ChatMessage) => ({
          ...m,
          message: (m as unknown as { message?: string; Message?: string }).message
            ?? (m as unknown as { Message?: string }).Message
            ?? '',
        })));
      })
      .catch(() => setMessages([]))
      .finally(() => setLoadingMessages(false));
  }, []);

  useEffect(() => {
    loadSections();
  }, [loadSections]);

  useEffect(() => {
    if (!selectedSectionId) return;
    loadMessages(selectedSectionId);
    const interval = setInterval(() => loadMessages(selectedSectionId), 5000);
    return () => clearInterval(interval);
  }, [selectedSectionId, loadMessages]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleSend = () => {
    if (!selectedSectionId || !newMessage.trim() || sending) return;
    setSending(true);
    apiClient.post(`/chat/sections/${selectedSectionId}`, { message: newMessage.trim() })
      .then(() => {
        setNewMessage('');
        loadMessages(selectedSectionId);
      })
      .finally(() => setSending(false));
  };

  const formatTime = (iso: string) => {
    try {
      return new Date(iso).toLocaleString();
    } catch {
      return iso;
    }
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px', height: 'calc(100vh - 120px)' }}>
      <div className="glass-panel" style={{ padding: '24px' }}>
        <h1 style={{ marginBottom: '8px' }}>Course Chat</h1>
        <p style={{ color: 'var(--text-muted)', margin: 0 }}>
          {role === 'student'
            ? 'Message your instructors for enrolled courses.'
            : 'Communicate with students in your sections.'}
        </p>
      </div>

      <div className="glass-panel" style={{ display: 'flex', flex: 1, minHeight: 0, overflow: 'hidden' }}>
        <div style={{
          width: '280px',
          borderRight: '1px solid var(--border-color)',
          overflowY: 'auto',
          padding: '12px',
        }}>
          <div style={{ fontWeight: 600, marginBottom: '12px', color: 'var(--text-muted)', fontSize: '0.85rem' }}>
            {role === 'student' ? 'My Courses' : 'My Sections'}
          </div>
          {loadingSections ? (
            <div style={{ color: 'var(--text-muted)' }}>Loading...</div>
          ) : sections.length === 0 ? (
            <div style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>
              No sections available.
            </div>
          ) : (
            sections.map((sec) => (
              <button
                key={sec.id}
                onClick={() => setSelectedSectionId(sec.id)}
                style={{
                  width: '100%',
                  textAlign: 'left',
                  padding: '12px',
                  marginBottom: '8px',
                  borderRadius: '8px',
                  border: selectedSectionId === sec.id
                    ? '1px solid var(--accent)'
                    : '1px solid var(--border-color)',
                  background: selectedSectionId === sec.id
                    ? 'rgba(59, 130, 246, 0.1)'
                    : 'rgba(255,255,255,0.02)',
                  color: 'var(--text-main)',
                  cursor: 'pointer',
                }}
              >
                <div style={{ fontWeight: 600 }}>{sec.courseCode}</div>
                <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>{sec.courseName}</div>
                {sec.instructorName && (
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-muted)', marginTop: '4px' }}>
                    {sec.instructorName}
                  </div>
                )}
              </button>
            ))
          )}
        </div>

        <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
          {selectedSection ? (
            <>
              <div style={{ padding: '16px', borderBottom: '1px solid var(--border-color)' }}>
                <div style={{ fontWeight: 600 }}>{selectedSection.courseCode} — {selectedSection.courseName}</div>
              </div>

              <div style={{ flex: 1, overflowY: 'auto', padding: '16px', display: 'flex', flexDirection: 'column', gap: '12px' }}>
                {loadingMessages && messages.length === 0 ? (
                  <div style={{ color: 'var(--text-muted)' }}>Loading messages...</div>
                ) : messages.length === 0 ? (
                  <div style={{ color: 'var(--text-muted)' }}>No messages yet. Start the conversation!</div>
                ) : (
                  messages.map((msg) => {
                    const isInstructor = msg.senderRole === 'instructor';
                    return (
                      <div
                        key={msg.id}
                        style={{
                          padding: '12px 16px',
                          borderRadius: '8px',
                          background: 'rgba(255,255,255,0.03)',
                          borderLeft: isInstructor ? '3px solid #3B82F6' : '3px solid transparent',
                        }}
                      >
                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '6px' }}>
                          <span style={{ fontWeight: 600, fontSize: '0.9rem' }}>{msg.senderName}</span>
                          <span style={{
                            fontSize: '11px',
                            padding: '2px 6px',
                            borderRadius: '4px',
                            background: isInstructor ? 'rgba(59, 130, 246, 0.2)' : 'rgba(148, 163, 184, 0.2)',
                            color: isInstructor ? '#3B82F6' : '#94A3B8',
                          }}>
                            {msg.senderRole}
                          </span>
                          <span style={{ fontSize: '11px', color: 'var(--text-muted)', marginLeft: 'auto' }}>
                            {formatTime(msg.sentAt)}
                          </span>
                        </div>
                        <div style={{ fontSize: '0.95rem' }}>{msg.message}</div>
                      </div>
                    );
                  })
                )}
                <div ref={messagesEndRef} />
              </div>

              <div style={{
                padding: '16px',
                borderTop: '1px solid var(--border-color)',
                display: 'flex',
                gap: '8px',
              }}>
                <input
                  type="text"
                  value={newMessage}
                  onChange={(e) => setNewMessage(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleSend()}
                  placeholder="Type a message..."
                  style={{
                    flex: 1,
                    padding: '10px 14px',
                    borderRadius: '8px',
                    border: '1px solid var(--border-color)',
                    background: 'rgba(255,255,255,0.03)',
                    color: 'var(--text-main)',
                  }}
                />
                <button
                  className="glass-btn primary"
                  onClick={handleSend}
                  disabled={sending || !newMessage.trim()}
                >
                  Send
                </button>
              </div>
            </>
          ) : (
            <div style={{ flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-muted)' }}>
              Select a section to view chat
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export const StudentChat: React.FC = () => <ChatPage role="student" />;
export const InstructorChat: React.FC = () => <ChatPage role="instructor" />;
