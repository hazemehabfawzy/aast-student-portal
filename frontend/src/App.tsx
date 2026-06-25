import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './auth/AuthContext';
import { ProtectedRoute } from './auth/ProtectedRoute';
import { AppLayout } from './pages/AppLayout';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';

// Student Pages
import { StudentProfile } from './pages/student/StudentProfile';
import { StudentResults } from './pages/student/StudentResults';
import { StudentSchedule } from './pages/student/StudentSchedule';
import { StudentRegister } from './pages/student/StudentRegister';
import { StudentNotifications } from './pages/student/StudentNotifications';

// Instructor Pages
import { InstructorSections } from './pages/instructor/InstructorSections';
import { InstructorAttendance } from './pages/instructor/InstructorAttendance';
import { InstructorGrading } from './pages/instructor/InstructorGrading';

// Admin Pages
import { AdminStudents } from './pages/admin/AdminStudents';
import { AdminCourses } from './pages/admin/AdminCourses';
import { AdminSections } from './pages/admin/AdminSections';
import { AdminPolicies } from './pages/admin/AdminPolicies';
import { AdminImport } from './pages/admin/AdminImport';
import { AdminReports } from './pages/admin/AdminReports';

function App() {
  const { loading } = useAuth();

  if (loading) {
    return (
      <div className="auth-page">
        <div className="auth-card glass-panel">
          <div className="brand-title">AAST PORTAL</div>
          <div className="brand-subtitle">Initializing Secure Authentication...</div>
          <div style={{ color: 'var(--text-muted)' }}>Please wait a moment.</div>
        </div>
      </div>
    );
  }

  return (
    <BrowserRouter>
      <Routes>
        {/* Public Routes */}
        <Route path="/login" element={<Login />} />
        <Route path="/dashboard" element={<Dashboard />} />

        {/* Protected Student Routes */}
        <Route element={<ProtectedRoute allowedRoles={['student']} />}>
          <Route element={<AppLayout />}>
            <Route path="/student/profile" element={<StudentProfile />} />
            <Route path="/student/results" element={<StudentResults />} />
            <Route path="/student/schedule" element={<StudentSchedule />} />
            <Route path="/student/register" element={<StudentRegister />} />
            <Route path="/student/notifications" element={<StudentNotifications />} />
          </Route>
        </Route>

        {/* Protected Instructor Routes */}
        <Route element={<ProtectedRoute allowedRoles={['instructor']} />}>
          <Route element={<AppLayout />}>
            <Route path="/instructor/sections" element={<InstructorSections />} />
            <Route path="/instructor/attendance" element={<InstructorAttendance />} />
            <Route path="/instructor/grading" element={<InstructorGrading />} />
          </Route>
        </Route>

        {/* Protected Admin Routes */}
        <Route element={<ProtectedRoute allowedRoles={['admin']} />}>
          <Route element={<AppLayout />}>
            <Route path="/admin/students" element={<AdminStudents />} />
            <Route path="/admin/courses" element={<AdminCourses />} />
            <Route path="/admin/sections" element={<AdminSections />} />
            <Route path="/admin/policies" element={<AdminPolicies />} />
            <Route path="/admin/import" element={<AdminImport />} />
            <Route path="/admin/reports" element={<AdminReports />} />
          </Route>
        </Route>

        {/* Fallback Redirection */}
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
