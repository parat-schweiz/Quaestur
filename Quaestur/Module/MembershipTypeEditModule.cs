using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MimeKit;
using SiteLibrary;

namespace Quaestur
{
    public class PaymentParameterEditViewModel : DialogViewModel
    {
        public string Key;
        public string Phrase;
        public string Value;

        public PaymentParameterEditViewModel(Translator translator, PaymentParameterType type, PaymentParameter parameter)
        {
            Key = type.Key.Replace(".", string.Empty).EscapeHtml();
            Phrase = type.GetTranslation(translator).EscapeHtml();
            Value = Math.Round(parameter.Value, 2).ToString();
        }
    }

    public class PaymentParametersEditViewModel : DialogViewModel
    {
        public string Id;
        public List<PaymentParameterEditViewModel> List;

        public PaymentParametersEditViewModel(Translator translator, MembershipType type)
            : base(translator, translator.Get("PaymentParameters.Edit.Title", "Title of the payment parameters edit dialog", "Edit payment parameters"), "paymentParametersEditDialog")
        {
            Id = type.Id.Value.ToString();
            var model = type.CreatePaymentModel();
            List = new List<PaymentParameterEditViewModel>();

            if (model != null)
            {
                List.AddRange(model.ParameterTypes
                    .Select(pt => new PaymentParameterEditViewModel(translator, pt, 
                        type.PaymentParameters.First(p => p.Key.Value == pt.Key))));
            }
        }
    }

    public class MembershipTypeEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string[] Right;
        public string Payment;
        public string Collection;
        public List<MultiItemViewModel> BillTemplateLatex;
        public List<NamedIntViewModel> Rights;
        public List<NamedIntViewModel> Payments;
        public List<NamedIntViewModel> Collections;
        public string PhraseFieldRight;
        public string PhraseFieldPayment;
        public string PhraseFieldCollection;
        public string PhraseTestBillCreate;

        public MembershipTypeEditViewModel()
        { 
        }

        public MembershipTypeEditViewModel(Translator translator)
            : base(translator, translator.Get("MembershipType.Edit.Title", "Title of the membership type edit dialog", "Edit membership type"), "membershipTypeEditDialog")
        {
            PhraseFieldRight = translator.Get("MembershipType.Edit.Field.Right", "Right field in the membership type edit dialog", "Membership rights").EscapeHtml();
            PhraseFieldPayment = translator.Get("MembershipType.Edit.Field.Payment", "Payment field in the membership type edit dialog", "Payment model").EscapeHtml();
            PhraseFieldCollection = translator.Get("MembershipType.Edit.Field.Collection", "Collection field in the membership type edit dialog", "Collection model").EscapeHtml();
            PhraseTestBillCreate = translator.Get("MembershipType.Edit.Button.TestBillCreate", "Button to test creating bill", "Test creating bill").EscapeHtml();
        }

        public MembershipTypeEditViewModel(Translator translator, IDatabase db, Session session, Organization organization)
            : this(translator)
        {
            Method = "add";
            Id = organization.Id.Value.ToString();
            Name = translator.CreateLanguagesMultiItem("MembershipType.Edit.Field.Name", "Name field in the membership type edit dialog", "Name ({0})", new MultiLanguageString());
            Right = new string[0];
            Payment = string.Empty;
            Collection = string.Empty;
            BillTemplateLatex = translator.CreateLanguagesMultiItem("MembershipType.Edit.Field.BillTemplateLatex", "Bill template LaTeX field in the membership type edit dialog", "Bill template LaTeX ({0})", new MultiLanguageString(), EscapeMode.Latex);
            Rights = new List<NamedIntViewModel>();
            Rights.Add(new NamedIntViewModel(translator, MembershipRight.Voting, false));
            Payments = new List<NamedIntViewModel>();
            Payments.Add(new NamedIntViewModel(translator, PaymentModel.None, false));
            Payments.Add(new NamedIntViewModel(translator, PaymentModel.Fixed, false));
            Collections = new List<NamedIntViewModel>();
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.None, false));
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.Direct, false));
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.ByParent, false));
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.BySub, false));
        }

        public MembershipTypeEditViewModel(Translator translator, IDatabase db, Session session, MembershipType membershipType)
            : this(translator)
        {
            Method = "edit";
            Id = membershipType.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("MembershipType.Edit.Field.Name", "Name field in the membership type edit dialog", "Name ({0})", membershipType.Name.Value);
            Right = new string[0];
            Payment = string.Empty;
            Collection = string.Empty;
            BillTemplateLatex = translator.CreateLanguagesMultiItem("MembershipType.Edit.Field.BillTemplateLatex", "Bill template LaTeX field in the membership type edit dialog", "Bill template LaTeX ({0})", membershipType.BillTemplateLatex.Value, EscapeMode.Latex);
            Rights = new List<NamedIntViewModel>();
            Rights.Add(new NamedIntViewModel(translator, MembershipRight.Voting, membershipType.Rights.Value.HasFlag(MembershipRight.Voting)));
            Payments = new List<NamedIntViewModel>();
            Payments.Add(new NamedIntViewModel(translator, PaymentModel.None, membershipType.Payment.Value == PaymentModel.None));
            Payments.Add(new NamedIntViewModel(translator, PaymentModel.Fixed, membershipType.Payment.Value == PaymentModel.Fixed));
            Collections = new List<NamedIntViewModel>();
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.None, membershipType.Collection.Value == CollectionModel.None));
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.Direct, membershipType.Collection.Value == CollectionModel.Direct));
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.ByParent, membershipType.Collection.Value == CollectionModel.ByParent));
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.BySub, membershipType.Collection.Value == CollectionModel.BySub));
        }
    }

    public class MembershipTypeViewModel : MasterViewModel
    {
        public string Id;

        public MembershipTypeViewModel(Translator translator, Session session, Organization organization)
            : base(translator, 
            translator.Get("MembershipType.List.Title", "Title of the membership type list page", "Membership type"), 
            session)
        {
            Id = organization.Id.Value.ToString();
        }
    }

    public class MembershipTypeListItemViewModel
    {
        public string Id;
        public string Name;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public MembershipTypeListItemViewModel(Translator translator, Session session, MembershipType membershipType)
        {
            Id = membershipType.Id.Value.ToString();
            Name = membershipType.Name.Value[translator.Language].EscapeHtml();
            Editable =
                session.HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationQuestion = translator.Get("MembershipType.List.Delete.Confirm.Question", "Delete membershipType confirmation question", "Do you really wish to delete membershipType {0}?", membershipType.GetText(translator)).EscapeHtml();
        }
    }

    public class MembershipTypeListViewModel
    {
        public string Id;
        public string Name;
        public string PhraseHeaderOrganization;
        public string PhraseHeaderPaymentParameters;
        public string PhraseHeaderBillTemplates;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<MembershipTypeListItemViewModel> List;
        public bool AddAccess;

        public MembershipTypeListViewModel(Translator translator, IDatabase database, Session session, Organization organization)
        {
            Id = organization.Id.Value.ToString();
            Name = organization.Name.Value[translator.Language].EscapeHtml();
            PhraseHeaderOrganization = translator.Get("MembershipType.List.Header.Organization", "Header part 'Organization' in the membership type list", "Organization").EscapeHtml();
            PhraseHeaderPaymentParameters = translator.Get("MembershipType.List.Header.PaymentParameters", "Link 'Payment parameters' caption in the membership type list", "Payment parameters").EscapeHtml();
            PhraseHeaderBillTemplates =
                session.HasAccess(organization, PartAccess.Structure, AccessRight.Write) ?
                translator.Get("MembershipType.List.Header.BillTemplates", "Link 'Bill templates' caption in the membership type list", "Bill templates").EscapeHtml() :
                string.Empty;
            PhraseDeleteConfirmationTitle = translator.Get("MembershipType.List.Delete.Confirm.Title", "Delete membership type confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("MembershipType.List.Delete.Confirm.Info", "Delete membership type confirmation info", "This will also delete all roles and permissions under that membershipType.").EscapeHtml();
            List = new List<MembershipTypeListItemViewModel>(
                organization.MembershipTypes
                .Where(mt => session.HasAccess(mt.Organization.Value, PartAccess.Structure, AccessRight.Read))
                .Select(mt => new MembershipTypeListItemViewModel(translator, session, mt))
                .OrderBy(mt => mt.Name));
            AddAccess = session.HasAccess(organization, PartAccess.Structure, AccessRight.Write);
        }
    }

    public class MembershipTypeEditModule : QuaesturModule
    {
        public MembershipTypeEditModule()
        {
            RequireCompleteLogin();

            Get["/membershiptype/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var organization = Database.Query<Organization>(idString);

                if (organization != null)
                {
                    return View["View/membershiptype.sshtml",
                        new MembershipTypeViewModel(Translator, CurrentSession, organization)];
                }

                return null;
            };
            Get["/membershiptype/list/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var organization = Database.Query<Organization>(idString);

                if (organization != null)
                {
                    return View["View/membershiptypelist.sshtml",
                        new MembershipTypeListViewModel(Translator, Database, CurrentSession, organization)];
                }

                return null;
            };
            Get["/membershiptype/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var membershipType = Database.Query<MembershipType>(idString);

                if (membershipType != null)
                {
                    return View["View/membershiptypeedit.sshtml",
                        new MembershipTypeEditViewModel(Translator, Database, CurrentSession, membershipType)];
                }

                return null;
            };
            Post["/membershiptype/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<MembershipTypeEditViewModel>(ReadBody());
                var membershipType = Database.Query<MembershipType>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(membershipType))
                {
                    if (status.HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Name", membershipType.Name, model.Name);
                        status.AssignFlagIntsString("Right", membershipType.Rights, model.Right);
                        status.AssignEnumIntString("Payment", membershipType.Payment, model.Payment);
                        status.AssignEnumIntString("Collection", membershipType.Collection, model.Collection);
                        status.AssignMultiLanguageFree("BillTemplateLatex", membershipType.BillTemplateLatex, model.BillTemplateLatex);

                        if (status.IsSuccess)
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                Database.Save(membershipType);
                                AddPaymentModelParameters(membershipType);
                                transaction.Commit();
                                Notice("{0} changed membership type {1}", CurrentSession.User.ShortHand, membershipType);
                            }
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/membershiptype/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var organization = Database.Query<Organization>(idString);

                if (organization != null)
                {
                    if (HasAccess(organization, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/membershipTypeedit.sshtml",
                            new MembershipTypeEditViewModel(Translator, Database, CurrentSession, organization)];
                    }
                }

                return null;
            };
            Post["/membershiptype/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var organization = Database.Query<Organization>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(organization))
                {
                    if (status.HasAccess(organization, PartAccess.Structure, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<MembershipTypeEditViewModel>(ReadBody());
                        var membershipType = new MembershipType(Guid.NewGuid());
                        status.AssignMultiLanguageRequired("Name", membershipType.Name, model.Name);
                        status.AssignFlagIntsString("Right", membershipType.Rights, model.Right);
                        status.AssignEnumIntString("Payment", membershipType.Payment, model.Payment);
                        status.AssignEnumIntString("Collection", membershipType.Collection, model.Collection);
                        status.AssignMultiLanguageFree("BillTemplateLatex", membershipType.BillTemplateLatex, model.BillTemplateLatex);
                        membershipType.Organization.Value = organization;

                        if (status.IsSuccess)
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                Database.Save(membershipType);
                                AddPaymentModelParameters(membershipType);
                                transaction.Commit();
                                Notice("{0} added membership type {1}", CurrentSession.User.ShortHand, membershipType);
                            }
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/membershiptype/parameters/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var membershipType = Database.Query<MembershipType>(idString);

                if (membershipType != null)
                {
                    if (HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/membershipedit_parameters.sshtml",
                            new PaymentParametersEditViewModel(Translator, membershipType)];
                    }
                }

                return null;
            };
            Post["/membershiptype/parameters/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var membershipType = Database.Query<MembershipType>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(membershipType))
                {
                    if (status.HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        var model = JObject.Parse(ReadBody());
                        var payment = membershipType.CreatePaymentModel();

                        if (payment != null)
                        {
                            foreach (var property in model.Properties())
                            {
                                var parameter = membershipType.PaymentParameters
                                    .First(p => p.Key.Value.Replace(".", string.Empty) == property.Name);

                                if (parameter != null)
                                {
                                    status.AssignDecimalString(property.Name, parameter.Value, property.Value.ToString());
                                }
                                else
                                {
                                    status.SetErrorNotFound();
                                }
                            }

                            if (status.IsSuccess)
                            {
                                Database.Save(membershipType);
                                Notice("{0} changed payment parameter types of {1}", CurrentSession.User.ShortHand, membershipType);
                            }
                        }
                        else
                        {
                            status.SetErrorNotFound();
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/membershiptype/delete/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var membershipType = Database.Query<MembershipType>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(membershipType))
                {
                    if (status.HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            membershipType.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted membership type {1}", CurrentSession.User.ShortHand, membershipType);
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Post["/membershiptype/testcreatebill/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var membershipType = Database.Query<MembershipType>(idString);
                var result = new PostResult();

                if (membershipType != null &&
                    HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Read))
                {
                    if (membershipType.Payment.Value != PaymentModel.None &&
                        membershipType.Collection.Value == CollectionModel.Direct)
                    {
                        var input = JObject.Parse(ReadBody());
                        var userLanguage = CurrentSession.User.Language.Value;

                        var membership = new Membership(Guid.NewGuid());
                        membership.Organization.Value = membershipType.Organization.Value;
                        membership.Type.Value = membershipType;
                        membership.Person.Value = CurrentSession.User;
                        membership.StartDate.Value = DateTime.UtcNow.AddDays(-10).Date;

                        var content = new Multipart("mixed");
                        var bodyText = Translate("MembershipType.TestCreateBill.Text", "Subject of the test create bill mail", "See attachements");
                        var bodyPart = new TextPart("plain") { Text = bodyText };
                            bodyPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
                        content.Add(bodyPart);

                        foreach (var language in new Language[] { Language.English, Language.French, Language.German, Language.Italian })
                        {
                            var propertyName = "L" + ((int)language).ToString();
                            if (input.Properties().Any(p => p.Name == propertyName))
                            {
                                var template = (string)input.Property(propertyName);
                                if (!string.IsNullOrEmpty(template) &&
                                    template.Length > 64)
                                {
                                    membership.Type.Value.BillTemplateLatex.Value[language] = template;
                                    CurrentSession.User.Language.Value = language;
                                    var billDocument = new BillDocument(Translator, Database, membership);

                                    if (billDocument.Create())
                                    {
                                        var documentStream = new MemoryStream(billDocument.Bill.DocumentData.Value);
                                        var documentPart = new MimePart("application", "pdf");
                                        documentPart.Content = new MimeContent(documentStream, ContentEncoding.Binary);
                                        documentPart.ContentType.Name = language.ToString() + ".bill.pdf";
                                        documentPart.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
                                        documentPart.ContentDisposition.FileName = language.ToString() + ".bill.pdf";
                                        documentPart.ContentTransferEncoding = ContentEncoding.Base64;
                                        content.Add(documentPart);
                                    }

                                    var latexPart = new TextPart("plain") { Text = billDocument.TexDocument };
                                    latexPart.ContentType.Name = language.ToString() + ".tex";
                                    latexPart.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
                                    latexPart.ContentDisposition.FileName = language.ToString() + ".tex";
                                    latexPart.ContentTransferEncoding = ContentEncoding.Base64;
                                    content.Add(latexPart);

                                    var errorPart = new TextPart("plain") { Text = billDocument.ErrorText };
                                    errorPart.ContentType.Name = language.ToString() + ".output.txt";
                                    errorPart.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
                                    errorPart.ContentDisposition.FileName = language.ToString() + ".output.txt";
                                    errorPart.ContentTransferEncoding = ContentEncoding.Base64;
                                    content.Add(errorPart);
                                }
                            }
                        }

                        if (content.Count > 1)
                        {
                            var to = new MailboxAddress(CurrentSession.User.ShortHand, CurrentSession.User.PrimaryMailAddress);
                            var subject = Translate("MembershipType.TestCreateBill.Subject", "Subject of the test create bill mail", "Test create bill");
                            Global.MailCounter.Used();
                            Global.Mail.Send(to, subject, content);

                            result.MessageType = "succss";
                            result.MessageText = Translate("MembershipType.TestCreateBill.Success", "Success during test create bill", "Compilation finished. You will recieve the output via mail.");
                            result.IsSuccess = true;
                        }
                        else
                        {
                            result.MessageType = "warning";
                            result.MessageText = Translate("MembershipType.TestCreateBill.Failed.Failed", "LaTeX failed during test create bill", "Compilation failed. No output was generated.");
                            result.IsSuccess = false;
                        }
                    }
                    else
                    {
                        result.MessageType = "warning";
                        result.MessageText = Translate("MembershipType.TestCreateBill.Failed.NoPayment", "LaTeX failed during test create bill", "No payment or billing was selected. No output was generated.");
                        result.IsSuccess = false;
                    }
                }
                else
                {
                    result.MessageType = "warning";
                    result.MessageText = Translate("MembershipType.TestCreateBill.Failed.NotFound", "Object not found during test create bill", "Object not found");
                    result.IsSuccess = false;
                }

                return JsonConvert.SerializeObject(result);
            };
        }

        private void AddPaymentModelParameters(MembershipType type)
        {
            var model = type.CreatePaymentModel();

            if (model != null)
            {
                foreach (var parameterType in model.ParameterTypes)
                {
                    var parameter = type.PaymentParameters.FirstOrDefault(p => p.Key.Value == parameterType.Key);

                    if (parameter == null)
                    {
                        parameter = new PaymentParameter(Guid.NewGuid());
                        parameter.Key.Value = parameterType.Key;
                        parameter.Value.Value = parameterType.DefaultValue;
                        parameter.Type.Value = type;
                        Database.Save(parameter);
                    }
                }
            }
        }
    }
}
