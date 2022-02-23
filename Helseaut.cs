using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Agresso.ServerExtension;
using Agresso.Interface.CommonExtension;
using HLS_HelseAut.Hprservice2;
using System.Text.RegularExpressions;
using System.IO;
using System.Configuration;

namespace HLS_HelseAut
{
    [ServerProgram("HELHPR")]
    public class Helseaut : ServerProgramBase
    {
        private static readonly Regex sWhitespace = new Regex(@"\s+");
      
        public static string ReplaceWhitespace(string input, string replacement)
        {
            return sWhitespace.Replace(input, replacement);
        }
        public override void Run()
        {

            IServerDbAPI api = ServerAPI.Current.DatabaseAPI;
            string username = ServerAPI.Current.Parameters["username"];
            string password = ServerAPI.Current.Parameters["password"];
            string client = ServerAPI.Current.Parameters["client"];
            string resourceid = ServerAPI.Current.Parameters["ressurs"];

            if (resourceid.Equals("*"))
                resourceid = "%";

            Me.API.WriteLog("Resource som blir sjekket {0}", resourceid);

            var _service = new Hprservice2.HPR2ServiceClient("WSHttpBinding_IHPR2Service");
            DateTime onDate = DateTime.Now;
            _service.ClientCredentials.UserName.UserName = ReplaceWhitespace(username, "");
            _service.ClientCredentials.UserName.Password = ReplaceWhitespace(password, "");
            try
            {
                //sletting av data
                IStatement sqldelete = CurrentContext.Database.CreateStatement();
                //sqldelete.Append("delete from ahsrelvalue where rel_attr_id in ('3310','3311','3312','3313') and client = 'S1'");
                sqldelete.Append("delete from ahsrelvalue where rel_attr_id in ('3310','3311','3312','3313','3314') and client = @client and resource_id like @resourceid");
                sqldelete["client"] = client;
                sqldelete["resourceid"] = resourceid;
                CurrentContext.Database.Execute(sqldelete);

                DataTable users = new DataTable("Users");
                IStatement sqlname = CurrentContext.Database.CreateStatement();
                sqlname.Append("select distinct a.social_sec, a.resource_id, a.client  from ahsresources a where a.resource_id like @resourceid and a.client = @client and  a.resource_id in (select resource_id from aprresourcepost where a.resource_id = resource_id  and left(type,1) in ('F','M') and a.client = client and getDate() between date_from and date_to)");
                sqlname["client"] = client;
                sqlname["resourceid"] = resourceid;
                CurrentContext.Database.Read(sqlname, users);
                foreach (DataRow user in users.Rows)
                {
                    var social_sec = user["social_sec"].ToString();  //"29068636661";
         
                    Task.Delay(200);
                    try
                    {
                    var hprPerson1 = _service.HentPersonMedPersonnummer(ReplaceWhitespace(user["social_sec"].ToString(), ""), onDate);

                    // legge til data hpr-nummer
                    IStatement sqlinsert10 = CurrentContext.Database.CreateStatement();
                    sqlinsert10.Append("insert into ahsrelvalue(client, date_from, date_to, description, last_update, percentage, period_from, period_to, rel_attr_id, rel_value, resource_id, status, user_id, value_1) Values(@client, @date_from, @date_to, @description, getDate(), 0, 0, 0, '3310', @hpr, @resource_id, 'N', 'HPRUSR', @hpr)");
                    sqlinsert10["client"] = user["client"].ToString();
                    sqlinsert10["date_from"] = DateTime.Parse("Jan 1, 1901");
                    sqlinsert10["date_to"] = DateTime.Parse("Dec 31, 2099");
                    sqlinsert10["description"] = "HPR-Nr";
                    sqlinsert10["resource_id"] = user["resource_id"].ToString();
                    sqlinsert10["hpr"] = hprPerson1.HPRNummer.ToString();
                    CurrentContext.Database.Execute(sqlinsert10);

                    foreach (var god in hprPerson1.Godkjenninger)
                    {
                        //if (god.Helsepersonellkategori.Aktiv == true)
                        //{
                        IStatement sqlinsert11 = CurrentContext.Database.CreateStatement();
                        sqlinsert11.Append("insert into ahsrelvalue(client, date_from,date_to,description,last_update, percentage,period_from,period_to,rel_attr_id,rel_value,resource_id,status,user_id,value_1) Values (@client,@date_from,@date_to,@description,getDate(),0,0,0,'3313',@val,@resource_id,'N','HPRUSR',@hpr)");
                        sqlinsert11["client"] = user["client"].ToString();
                        sqlinsert11["date_from"] = god.Gyldig.Fra;
                        sqlinsert11["date_to"] = god.Gyldig.Til;
                        sqlinsert11["description"] = god.Helsepersonellkategori.Beskrivelse;
                        sqlinsert11["resource_id"] = user["resource_id"].ToString();
                        sqlinsert11["val"] = god.Helsepersonellkategori.Verdi;
                        sqlinsert11["hpr"] = hprPerson1.HPRNummer.ToString();
                        CurrentContext.Database.Execute(sqlinsert11);


                        //}
                        //if (god.Autorisasjon.Aktiv == true)
                        //{
                        IStatement sqlinsert12 = CurrentContext.Database.CreateStatement();
                        sqlinsert12.Append("insert into ahsrelvalue(client, date_from,date_to,description,last_update, percentage,period_from,period_to,rel_attr_id,rel_value,resource_id,status,user_id,value_1) Values (@client,@date_from,@date_to,@description,getDate(),0,0,0,'3312',@val,@resource_id,'N','HPRUSR',@hpr)");
                        sqlinsert12["client"] = user["client"].ToString();
                        sqlinsert12["date_from"] = god.Gyldig.Fra;
                        sqlinsert12["date_to"] = god.Gyldig.Til;
                        sqlinsert12["description"] = god.Autorisasjon.Beskrivelse;
                        sqlinsert12["resource_id"] = user["resource_id"].ToString();
                        sqlinsert12["val"] = god.Autorisasjon.Verdi.ToString();
                        sqlinsert12["hpr"] = hprPerson1.HPRNummer.ToString();
                        CurrentContext.Database.Execute(sqlinsert12);
                            //}
                            foreach (var spes in god.Suspensjonsperioder)
                        {
                            IStatement sqlinsert13 = CurrentContext.Database.CreateStatement();
                            sqlinsert13.Append("insert into ahsrelvalue(client, date_from,date_to,description,last_update, percentage,period_from,period_to,rel_attr_id,rel_value,resource_id,status,user_id,value_1) Values (@client,@date_from,@date_to,@description,getDate(),0,0,0,'3311',@val,@resource_id,'N','HPRUSR',@hpr)");
                            sqlinsert13["client"] = user["client"].ToString();
                            sqlinsert13["date_from"] = spes.Periode.Fra;
                            sqlinsert13["date_to"] = spes.Periode.Til;
                            sqlinsert13["description"] = "Suspansjon";
                            sqlinsert13["resource_id"] = user["resource_id"].ToString();
                            sqlinsert13["val"] = spes.Id.ToString();
                            sqlinsert13["hpr"] = hprPerson1.HPRNummer.ToString();
                            CurrentContext.Database.Execute(sqlinsert13);
                        }
                        foreach (var uten in god.Utdannelser)
                        {

                            IStatement sqlinsert14 = CurrentContext.Database.CreateStatement();
                            sqlinsert14.Append("insert into ahsrelvalue(client, date_from,date_to,description,last_update, percentage,period_from,period_to,rel_attr_id,rel_value,resource_id,status,user_id,value_1) Values (@client,@date_from,@date_to,@description,getDate(),0,0,0,3314,@val,@resource_id,'N','HPRUSR',@hpr)");

                            sqlinsert14["client"] =user["client"].ToString();
                            sqlinsert14["date_from"] = DateTime.Parse("Jan 1, 1901");
                            sqlinsert14["date_to"] = DateTime.Parse("Dec 31, 2099");
                            sqlinsert14["description"]=  uten.Type.Beskrivelse;
                            sqlinsert14["resource_id"]= user["resource_id"].ToString();
                            sqlinsert14["val"]= uten.Type.Verdi;
                            sqlinsert14["hpr"] = hprPerson1.HPRNummer.ToString();
                            CurrentContext.Database.Execute(sqlinsert14);
                        }
                    }
                }
                        catch
                        {
                         Me.API.WriteLog("Ikke hpr");
                       }
                    }
            }
            catch (IOException e)
            {
                // Extract some information from this exception, and then
                // throw it to the parent method.
                if (e.Source != null)
                    Me.API.WriteLog("IOException source: {0}", e.Source);
                Me.API.WriteLog("Feil i henting");
            }


        }
    }
}
