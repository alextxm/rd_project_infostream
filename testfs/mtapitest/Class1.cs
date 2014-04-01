using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using mtapitest.MantisConnect;

namespace mtapitest
{
    public class TicketInfo
    {
        public int Id { get; set; }
        public string Summary { get; set; }
        public string Status { get; set; }
    }

    public class ProjectInfo
    {
        public int Id { get; set; }
        public string ProjectName { get; set; }
    }

    public class AccountInfo
    {

    }

    public class TicketInfo
    {
          public int Id { get; set; }
          public string View_state" type="tns:ObjectRef" />
          public DateTime Last_updated { get; set; }
          public ProjectInfo ProjectProjectInfo
          public string CategoryProjectInfo
          public GenericMantisTypeInfo Priority" type="tns:ObjectRef" />
          public GenericMantisTypeInfo Severity" type="tns:ObjectRef" />
          public GenericMantisTypeInfo Status" type="tns:ObjectRef" />
          public AccountInfo Reporter { get; set; }
          public string Summary { get; set; }
          public string Version { get; set; }
          public string Build { get; set; }
          public string Platform { get; set; }
          public string Os { get; set; }
          public string Os_build { get; set; }
          public string Reproducibility" type="tns:ObjectRef" />
          public DateTime Date_submitted { get; set; }
          public AccountInfo Handler" type="tns:AccountData" />
          public string Projection" type="tns:ObjectRef" />
          public string Eta" type="tns:ObjectRef" />
          public GenericMantisTypeInfo Resolution" type="tns:ObjectRef" />
          public string Fixed_in_version { get; set; }
          public string Target_version { get; set; }
          public string Description { get; set; }
          public string Steps_to_reproduce { get; set; }
          public string Additional_information { get; set; }
          public string Attachments" type="tns:AttachmentDataArray" />
          public string Relationships" type="tns:RelationshipDataArray" />
          public string Notes" type="tns:IssueNoteDataArray" />
          public string Due_date { get; set; }
    }

    public class GenericMantisTypeInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class MantisIntegration : CaringService
    {
        MantisConnect.MantisConnectPortTypeClient cli = null;
        MantisConnect.UserData user = null;

        public MantisIntegration(string username, System.Security.SecureString password)
            : base(username, password)
        {
            cli = new MantisConnect.MantisConnectPortTypeClient();
        }

        public bool Login()
        {
            if (user != null)
                user = null;

            user = cli.mc_login(username, password.ToString());
            if (user == null)
                return false;

            return true;
        }

        /// <summary>
        /// get available projects for current logged user
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ProjectInfo> GetProjects()
        {
            if (user == null)
                return null;

            try
            {
                return cli.mc_projects_get_user_accessible(username, password.ToString())
                            .ToList()
                            .ConvertAll<ProjectInfo>(p => new ProjectInfo() { Id = Convert.ToInt32(p.id), ProjectName = p.name });
            }
            catch
            {
            }

            return null;
        }

        /// <summary>
        /// get tickets for the specified project
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public IEnumerable<TicketInfo> GetTickets(int projectId, int status, int skip, int take)
        {
            try
            {

                cli.mc_enum_priorities
                cli.mc_project_get_issues(username, password.ToString(), projectId, )
                    
            }
            catch
            {

            }
        }
    }
}

