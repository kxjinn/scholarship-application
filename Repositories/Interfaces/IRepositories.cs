using scholarship_Application.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace scholarship_Application.Repositories.Interfaces
{
    public interface IRoleRepository
    {
        List<Role> GetAll();
        Role GetById(int id);
    }

    public interface IUserRepository
    {
        int AddUser(User user);
        void UpdateUser(User user);                              // full name + role only ang pwedeng i-edit/update
        void SetUserActive(int userId, bool isActive);
        void ResetPassword(int userId, string newPassword);
        List<User> GetAll();
        User GetById(int id);
        User GetActiveByUsername(string username);
        bool UsernameExists(string username, int excludeUserId = 0);
    }

    public interface ICourseRepository
    {
        int AddCourse(Course course);
        void UpdateCourse(Course course);
        void DeleteCourse(int courseId);
        List<Course> GetAll();
        Course GetById(int id);
        bool CodeExists(string courseCode, int excludeCourseId = 0);
        bool IsCourseInUse(int courseId);
    }

    public interface IRequirementRepository
    {
        int AddRequirement(Requirement requirement);
        void UpdateRequirement(Requirement requirement);
        void DeleteRequirement(int requirementId);
        List<Requirement> GetAll();
        Requirement GetById(int id);
        bool NameExists(string name, int excludeRequirementId = 0);
        bool IsRequirementInUse(int requirementId);
    }

    public interface IApplicantRepository
    {
        int AddApplicant(Applicant applicant);
        void UpdateApplicant(Applicant applicant);
        void SetApplicantActive(int applicantId, bool isActive);
        List<Applicant> Search(string keyword, bool activeOnly);
        Applicant GetById(int id);
        bool StudentNoExists(string studentNo, int excludeApplicantId = 0);
    }

    public interface IScholarshipRepository
    {
        int AddScholarship(Scholarship scholarship);
        void UpdateScholarship(Scholarship scholarship);
        void SetScholarshipActive(int scholarshipId, bool isActive);
        List<Scholarship> GetAll(bool activeOnly);
        List<Scholarship> GetOpenOn(DateTime date);
        Scholarship GetById(int id);
        List<Requirement> GetRequirements(int scholarshipId);
        bool NameExists(string name, int excludeScholarshipId = 0);
    }

    public interface IApplicationRepository
    {
        int AddApplication(ScholarshipApplication application);
        void UpdateApplication(ScholarshipApplication application);
        void ChangeStatus(int applicationId, ApplicationStatus newStatus, int changedBy, string remarks);

        List<ScholarshipApplication> Search(ApplicationFilter filter);
        ScholarshipApplication GetById(int id);
        bool Exists(int applicantId, int scholarshipId, string academicYear);
        int GetApprovedCount(int scholarshipId, string academicYear);

        List<StatusHistoryEntry> GetHistory(int applicationId);

        List<ApplicationRequirement> GetChecklist(int applicationId);
        void SetRequirementSubmitted(int applicationId, int requirementId, bool isSubmitted, int userId);
        int CountMissingRequirements(int applicationId);
    }

    public interface IReportRepository
    {
        DataTable GetApplicationSummary(int? scholarshipId, string academicYear);
        DataTable GetApprovedGrantees(int? scholarshipId, string academicYear);
        DataTable GetMissingRequirements(int? scholarshipId, string academicYear);
    }
}
