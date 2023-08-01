using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SP = Microsoft.SharePoint.Client;
using System.Security;

namespace ConsoleApp1
{
    class Program
    {
        static string srcSiteURL = "http://sharepointflexi/";
        static string srcListName = "eInvoice-IBKC";
        static string srcListInternalName = "eInoiceIB";

        static string dstSiteURL = "https://test-xxx.domain.com/";
        static string dstListName = "Invoices";

        static void Main(string[] args)
        {
            SP.ListItemCollectionPosition position = null;
            var page = 1;
            string RowLimit = "100";
            int itemCount = 0;

            //string password = "xxxx";
            //string account = "email@email.com";
            //var secret = new SecureString();
            //foreach (char c in password)
            //{
            //    secret.AppendChar(c);
            //}
            //using (SP.ClientContext ctx = new SP.ClientContext("https://xxxx.sharepoint.com/sites/NitinTestFlexi/"))
            //{

            //    ctx.Credentials = new SP.SharePointOnlineCredentials(account, secret);
            //    ctx.Load(ctx.Web);
            //    ctx.ExecuteQuery();
            //    SP.List topicsList = ctx.Web.Lists.GetByTitle("testlist");

            //    SP.ListItemCreationInformation oListItemCreationInformation = new SP.ListItemCreationInformation();
            //    SP.ListItem oListItem = topicsList.AddItem(oListItemCreationInformation);
            //    oListItem["Title"] = "New List Item";
            //    oListItem["Column1"] = "Test1";
            //    oListItem.Update();
            //    ctx.ExecuteQuery();
            //};

            try
            {
                Logger.LogInfo("Started");
                Console.WriteLine("Started");

                Console.WriteLine("Source Site: " + srcSiteURL);
                Console.WriteLine("Target Site: " + dstSiteURL);
                //Load Source Site
                SP.ClientContext srcContext = new SP.ClientContext(srcSiteURL);
                SP.Web srcWeb = srcContext.Web;
                SP.List srcList = GetList(srcContext, srcWeb, srcListName);

                //Get All Fields
                //srcContext.Load(srcList.Fields);
                //// We must call ExecuteQuery before enumerate list.Fields.
                //srcContext.ExecuteQuery();
                //List<SP.Field> fields = srcList.Fields.ToList();
                //SP.Field[] fArr = fields.ToArray();
                //foreach (SP.Field field in srcList.Fields)
                //{
                //    Console.WriteLine(field.Title + "|" + field.InternalName + "|" + field.TypeDisplayName);
                //}

                SP.ClientContext dstContext = new SP.ClientContext(dstSiteURL);
                SP.Web dstWeb = dstContext.Web;
                SP.List targetList = GetList(dstContext, dstWeb, dstListName);


                //Get Items from Source List
                do
                {
                    SP.CamlQuery query = new SP.CamlQuery();
                    try
                    {
                        query.ViewXml = @"<View Scope='RecursiveAll'><Query></Query><RowLimit>" + RowLimit + "</RowLimit></View>";
                        query.ListItemCollectionPosition = position;
                        SP.ListItemCollection listItems = srcList.GetItems(query);
                        srcContext.Load(listItems);
                        srcContext.ExecuteQuery();
                        Logger.LogInfo(listItems.Count() + ", " + srcList.Title);
                        Console.WriteLine(listItems.Count() + ", " + srcList.Title);
                        position = listItems.ListItemCollectionPosition;

                        foreach (var srcListItem in listItems)
                        {
                            itemCount++;
                            Console.WriteLine(itemCount + ", Processing Item ID:" + srcListItem["ID"]);
                            Logger.LogInfo(itemCount + ", Processing Item ID:" + srcListItem["ID"]);
                            //Test - Get Single Item by ID
                            //SP.ListItem srcListItem = srcList.GetItemById(39595);
                            //srcContext.Load(srcListItem);
                            //srcContext.ExecuteQuery();

                            //Get Attachments
                            bool hasAttachments = false;
                            SP.FileCollection itemAttachments = null;

                            //Console.WriteLine(srcListItem["ID"] + ", " + srcListItem["Title"]);
                            string src = string.Format("{0}Lists/{1}/Attachments/{2}/", srcSiteURL, srcListInternalName, srcListItem["ID"]);
                            SP.Folder folder = srcWeb.GetFolderByServerRelativeUrl(src);
                            srcContext.Load(folder);

                            try
                            {
                                srcContext.ExecuteQuery();
                                hasAttachments = true;
                            }
                            catch (SP.ServerException ex)
                            {
                                hasAttachments = false;
                                //Console.WriteLine(ex.Message);
                                Logger.LogInfo("No Attachment for ID " + srcListItem["ID"].ToString());
                                Console.BackgroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("No Attachment for ID " + srcListItem["ID"].ToString());
                            }
                            finally
                            {
                                Console.ResetColor();
                            }

                            if (hasAttachments)
                            {
                                itemAttachments = folder.Files;
                                srcContext.Load(itemAttachments);
                                srcContext.ExecuteQuery();
                            }

                            //Create and Add item in Target List
                            CreateListItem(dstContext, dstWeb, srcListItem, itemAttachments, hasAttachments, srcContext);
                        }
                        page++;
                    }
                    catch (Exception exDo)
                    {
                        Logger.LogException(exDo);
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine(exDo);
                    }
                    finally
                    {
                        Console.ResetColor();
                    }
                }
                //while (page < 2);
                while (position != null);

            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
            }
            finally
            {
                Console.ResetColor();
            }

            Console.WriteLine("Completed");
            Console.ReadLine();
        }

        static bool CreateListItem(SP.ClientContext dstContext, SP.Web web, SP.ListItem srcListItem, SP.FileCollection itemAttachments, bool HasAttachments, SP.ClientContext srcContext)
        {
            try
            {
                //Create and Add New Item
                SP.List destlist = web.Lists.GetByTitle(dstListName);
                SP.ListItemCreationInformation itemCreateInfo = new SP.ListItemCreationInformation();
                SP.ListItem newItem = destlist.AddItem(itemCreateInfo);
                newItem["ID_"] = srcListItem["ID"];
                newItem["Title"] = srcListItem["Title"];
                newItem["CreatedDate"] = srcListItem["Created"];
                newItem["CreatedBy"] = ((SP.FieldUserValue)srcListItem.FieldValues["Author"]).LookupValue;
                newItem["BatchNumber"] = srcListItem["BatchNumber"];
                newItem["InvoiceNumber"] = srcListItem["InvoiceNumber"];
                newItem["InvoiceDescription"] = srcListItem["InvoiceDescription"];
                newItem["InvoiceAmount"] = srcListItem["InvoiceAmount"];
                newItem["InvoiceDate"] = srcListItem["InvoiceDate"];
                newItem["eInvoicePaymentAmount"] = srcListItem["eInvoicePaymentAmount"];            //Latest Payment Amount
                newItem["eInvoicePaymentDate"] = srcListItem["eInvoicePaymentDate"];                //Latest Payment Date
                newItem["eInvoicePaymentNumber"] = srcListItem["eInvoicePaymentNumber"];            //Latest Payment Number
                newItem["dGLAccount"] = srcListItem["dGLAccount"];
                newItem["VendorID"] = srcListItem["VendorID"];                                      //Vendor Code
                newItem["VendorName"] = srcListItem["VendorName"];                                  //Vendor Name
                newItem["ApprovedDate"] = srcListItem["ApprovedDate"];                              //Approved Date
                newItem["eInvoiceBankID"] = srcListItem["eInvoiceBankID"];                          //Bank Code
                newItem["eInvoiceBankAccountID"] = srcListItem["eInvoiceBankAccountID"];            //Bank Account Code
                newItem["dAmount"] = srcListItem["dAmount"];                                        //Invoice Currency GL Distribution Line Amount
                newItem["eInvoicePayeeLocationID"] = srcListItem["eInvoicePayeeLocationID"];        //Vendor Remit To Code
                newItem["eInvoicePayeeName"] = srcListItem["eInvoicePayeeName"];                    //Payee Name
                newItem["eInvoiceLiabilityGLAccount"] = srcListItem["eInvoiceLiabilityGLAccount"];  //Liability GL Account
                newItem["eInvoicePaymentMethodID"] = srcListItem["eInvoicePaymentMethodID"];        //Payment Method
                newItem["eInvoiceVoucherNumber"] = srcListItem["eInvoiceVoucherNumber"];            //Voucher Number
                newItem["Invoice_x0020_Company_x0020_Code"] = srcListItem["InvoiceCompany"];                          //Invoice Company Code
                newItem.Update();
                dstContext.ExecuteQuery();
                Console.WriteLine("New Item Created. Source ID: " + srcListItem["ID"]);
                Logger.LogInfo("New Item Created. Source ID: " + srcListItem["ID"]);
                try
                {
                    if ((null != itemAttachments) && (itemAttachments.Count > 0))
                    {
                        AddAttachments(dstContext, newItem, itemAttachments, srcContext);
                        Console.WriteLine("Attachments Added. Source ID: " + srcListItem["ID"]);
                    }
                }
                catch (Exception exAttach)
                {
                    Logger.LogException(exAttach);
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine(exAttach);
                }
                finally
                {
                    Console.ResetColor();
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                //return false;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                return false;
            }
            finally
            {
                Console.ResetColor();
            }

            //return false;
        }

        static void AddAttachments(SP.ClientContext dstContext, SP.ListItem newItem, SP.FileCollection attachments, SP.ClientContext srcContext)
        {
            try
            {
                foreach (SP.File attachment in attachments)
                {
                    try
                    {
                        SP.ClientResult<Stream> clientResultStream = attachment.OpenBinaryStream();
                        srcContext.ExecuteQuery();
                        Stream stream = clientResultStream.Value;

                        //New Attachment Object for Target List Item
                        SP.AttachmentCreationInformation attachFileInfo = new SP.AttachmentCreationInformation();
                        Byte[] buffer = new Byte[attachment.Length];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        System.IO.MemoryStream stream2 = new System.IO.MemoryStream(buffer);
                        attachFileInfo.ContentStream = stream2;
                        attachFileInfo.FileName = attachment.Name;
                        SP.Attachment a = newItem.AttachmentFiles.Add(attachFileInfo);
                        dstContext.Load(a);
                        dstContext.ExecuteQuery();
                        stream2.Close();
                    }
                    catch (Exception exAttach)
                    {
                        Logger.LogException("", "Error Adding attachments");
                        Logger.LogException(exAttach);
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine(exAttach);
                    }
                    finally
                    {
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
            }
            finally
            {
                Console.ResetColor();
            }
        }

        static SP.List GetList(SP.ClientContext Context, SP.Web web, string ListName)
        {
            try
            {
                Context.Load(web.Lists);
                Context.ExecuteQuery();
                SP.List list = web.Lists.GetByTitle(ListName);
                Context.Load(list);
                Context.ExecuteQuery();
                return list;

            }
            catch (Exception ex)
            {
                Logger.LogInfo("Site: "+web.Url);
                Logger.LogException(ex);
                Console.WriteLine("Site: "+web.Url);
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                return null;
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}
