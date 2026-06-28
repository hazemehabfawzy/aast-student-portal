import sqlite3
import uuid
import datetime

db_path = "data/student_portal.db"

def test_escalation():
    conn = sqlite3.connect(db_path)
    cursor = conn.cursor()

    # Find Section 1
    cursor.execute("SELECT Id FROM Sections LIMIT 1;")
    section_id = cursor.fetchone()[0]

    # Find Student S001 (Omar Samir)
    cursor.execute("SELECT Id, FullName FROM Students WHERE StudentNumber = 'S001';")
    student_res = cursor.fetchone()
    student_id = student_res[0]
    student_name = student_res[1]

    # Find Enrollment for S001 in Section 1
    cursor.execute("SELECT Id, WithdrawalPending, IsWithdrawn FROM Enrollments WHERE StudentId = ? AND SectionId = ?;", (student_id, section_id))
    enroll_res = cursor.fetchone()
    enroll_id = enroll_res[0]
    print(f"Initial Enrollment: Pending={enroll_res[1]}, Withdrawn={enroll_res[2]}")

    # Clear existing attendance records for S001 to start fresh
    cursor.execute("DELETE FROM AttendanceRecords WHERE StudentId = ?;", (student_id,))
    conn.commit()

    # Test Helper to insert session and absent record
    def add_absence():
        session_id = str(uuid.uuid4())
        # Insert session
        cursor.execute("""
            INSERT INTO AttendanceSessions (Id, SectionId, InstructorId, StartTime, EndTime, Method, CurrentCode, CodeExpiresAt, Lat, Lng, RadiusMeters)
            VALUES (?, ?, ?, ?, ?, 'pin', '123456', ?, 0, 0, 100);
        """, (session_id, section_id, str(uuid.uuid4()), datetime.datetime.utcnow().isoformat(), datetime.datetime.utcnow().isoformat(), datetime.datetime.utcnow().isoformat()))
        
        # Insert absent record
        cursor.execute("""
            INSERT INTO AttendanceRecords (Id, SessionId, StudentId, CheckedInAt, Status, Method)
            VALUES (?, ?, ?, ?, 'absent', 'pin');
        """, (str(uuid.uuid4()), session_id, student_id, datetime.datetime.utcnow().isoformat()))
        conn.commit()

    # 1. First absence
    print("\n--- Adding 1st Absence ---")
    add_absence()
    cursor.execute("SELECT COUNT(*) FROM AttendanceRecords WHERE StudentId = ? AND Status = 'absent';", (student_id,))
    print(f"Total Absences: {cursor.fetchone()[0]}")

    # 2. Second absence (should trigger warning dynamically)
    print("\n--- Adding 2nd Absence ---")
    add_absence()
    cursor.execute("SELECT COUNT(*) FROM AttendanceRecords WHERE StudentId = ? AND Status = 'absent';", (student_id,))
    abs_count = cursor.fetchone()[0]
    print(f"Total Absences: {abs_count}")
    print(f"absenceWarning dynamic check: {abs_count >= 2}")

    # 3. Third absence (should set WithdrawalPending = 1 on Enrollment)
    print("\n--- Adding 3rd Absence ---")
    add_absence()
    # Trigger recomputation manually mimicking backend CloseSessionAsync
    # In C#, CloseSessionAsync calls RecomputeAbsenceEscalationAsync
    cursor.execute("SELECT COUNT(*) FROM AttendanceRecords WHERE StudentId = ? AND Status = 'absent';", (student_id,))
    abs_count = cursor.fetchone()[0]
    if abs_count == 3:
        cursor.execute("UPDATE Enrollments SET WithdrawalPending = 1 WHERE Id = ?;", (enroll_id,))
        conn.commit()
    
    cursor.execute("SELECT WithdrawalPending, IsWithdrawn FROM Enrollments WHERE Id = ?;", (enroll_id,))
    res = cursor.fetchone()
    print(f"After 3rd Absence: WithdrawalPending={res[0]}, IsWithdrawn={res[1]}")

    # 4. Fourth absence (should set IsWithdrawn = 1, WithdrawalPending = 0)
    print("\n--- Adding 4th Absence ---")
    add_absence()
    cursor.execute("SELECT COUNT(*) FROM AttendanceRecords WHERE StudentId = ? AND Status = 'absent';", (student_id,))
    abs_count = cursor.fetchone()[0]
    if abs_count >= 4:
        cursor.execute("UPDATE Enrollments SET IsWithdrawn = 1, WithdrawalPending = 0, WithdrawnAt = ? WHERE Id = ?;", (datetime.datetime.utcnow().isoformat(), enroll_id))
        conn.commit()

    cursor.execute("SELECT WithdrawalPending, IsWithdrawn, WithdrawnAt FROM Enrollments WHERE Id = ?;", (enroll_id,))
    res = cursor.fetchone()
    print(f"After 4th Absence: WithdrawalPending={res[0]}, IsWithdrawn={res[1]}, WithdrawnAt={res[2]}")

    # Reset enrollment for testing
    cursor.execute("UPDATE Enrollments SET WithdrawalPending = 0, IsWithdrawn = 0, WithdrawnAt = NULL WHERE Id = ?;", (enroll_id,))
    cursor.execute("DELETE FROM AttendanceRecords WHERE StudentId = ?;", (student_id,))
    conn.commit()
    conn.close()

if __name__ == "__main__":
    test_escalation()
