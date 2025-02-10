using FluentEmail.Core;
using Fluid;
using Microsoft.Extensions.Logging;
using Nxnw.Adc.Acm.Core.Common;
using Nxnw.Adc.BusinessEntity.Infrastructure.Interfaces;
using Nxnw.Adc.Common.ServiceModel;
using Nxnw.Adc.Communication.Infrastructure.Interfaces;
using Nxnw.Adc.Communication.ServiceModel;
using Nxnw.Adc.CountryCulture.Infrastructure.Interfaces;
using Nxnw.Adc.DataLayer.EntityClasses;
using Nxnw.Adc.Infrastructure;
using Nxnw.Adc.Infrastructure.Extensions;
using Nxnw.Adc.OrderEntry.Infrastructure.Interfaces;
using Nxnw.Adc.OrderEntry.ServiceModel;
using Nxnw.Adc.ServiceBus.Infrastructure.Interfaces;
using Nxnw.Adc.Utility.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nxnw.Adc.Communication.BusinessLogic
{
    public class DocumentGenerationBO : IDocumentGenerationBO
    {
        private static readonly FluidParser _FluidParser = new FluidParser();
        private readonly IAppDocumentRequestBO _AppDocumentRequestBO;
        private readonly IBusinessEntityBOCore _BusinessEntityBOCore;
        private readonly INlsCultureBO _NlsCultureBO;
        private readonly IAppDocumentRequestRcptBO _AppDocumentRequestRcptBO;
        private readonly ITransactionScopeProvider _TransactionScopeProvider;
        private readonly IMassTransitPublisherBO _MassTransitPublisherBO;
        private readonly ILogger<DocumentGenerationBO> _Logger;
        private readonly IFluentEmail _FluentEmail;
        private readonly IBusinessEntityCommunicationBO _BusinessEntityCommunicationBO;
        private readonly INlsDocumentTemplateBO _NlsDocumentTemplateBO;
        private readonly IAppDocumentTemplateBO _AppDocumentTemplateBO;
        private readonly IUtlSchemaBO _UtlSchemaBO;
        private readonly IUtlSchemaTypeBO _UtlSchemaTypeBO;
        private readonly ITtgApplicationSettings _TtgApplicationSettings;
        private readonly IAOEOrderBO _AOEOrderBO;
        private readonly IAIWrapperFactory _AIWrapperFactory;
        private readonly INlsAddressBO _NlsAddressBO;

        public DocumentGenerationBO(IAppDocumentRequestBO appDocumentRequestBO,
            IBusinessEntityBOCore businessEntityBOCore,
            INlsCultureBO nlsCultureBO,
            IAppDocumentRequestRcptBO appDocumentRequestRcptBO,
            ITransactionScopeProvider transactionScopeProvider,
            IMassTransitPublisherBO massTransitPublisherBO,
            ILogger<DocumentGenerationBO> logger,
            IFluentEmail fluentEmail,
            IBusinessEntityCommunicationBO businessEntityCommunicationBO,
            INlsDocumentTemplateBO nlsDocumentTemplateBO,
            IAppDocumentTemplateBO appDocumentTemplateBO,
            IUtlSchemaBO utlSchemaBO,
            IUtlSchemaTypeBO utlSchemaTypeBO,
            ITtgApplicationSettings ttgApplicationSettings,
            IAOEOrderBO aOEOrderBO,
            IAIWrapperFactory aiWrapperFactory,
            INlsAddressBO nlsAddressBO)
        {
            _AppDocumentRequestBO = appDocumentRequestBO;
            _BusinessEntityBOCore = businessEntityBOCore;
            _NlsCultureBO = nlsCultureBO;
            _AppDocumentRequestRcptBO = appDocumentRequestRcptBO;
            _TransactionScopeProvider = transactionScopeProvider;
            _MassTransitPublisherBO = massTransitPublisherBO;
            _Logger = logger;
            _FluentEmail = fluentEmail;
            _BusinessEntityCommunicationBO = businessEntityCommunicationBO;
            _NlsDocumentTemplateBO = nlsDocumentTemplateBO;
            _AppDocumentTemplateBO = appDocumentTemplateBO;
            _UtlSchemaBO = utlSchemaBO;
            _UtlSchemaTypeBO = utlSchemaTypeBO;
            _TtgApplicationSettings = ttgApplicationSettings;
            _AOEOrderBO = aOEOrderBO;
            _AIWrapperFactory = aiWrapperFactory;
            _NlsAddressBO = nlsAddressBO;
        }

        public void Request(AppDocumentTemplateRequest appDocumentTemplateRequest)
        {
            AppDocumentTemplateEntity appDocumentTemplateEntity = _AppDocumentTemplateBO.GetAppDocumentTemplateEntity(appDocumentTemplateRequest.TemplateName);
            UtlSchemaEntity utlSchemaEntity = _UtlSchemaBO.GetUtlSchemaEntity(appDocumentTemplateEntity.UtlSchemaGuid.Value);
            UtlSchemaTypeEntity utlSchemaTypeEntity = _UtlSchemaTypeBO.GetUtlSchemaTypeEntity(utlSchemaEntity.UtlSchemaTypeGuid);

            BeeBusinessEntity businessEntity = _BusinessEntityBOCore.GetBeeBusinessEntityWithBeeEntityEntity(appDocumentTemplateRequest.GenPlanGuid, appDocumentTemplateRequest.BeeNumber.Value);
            NlsCultureEntity cultureEntity = _NlsCultureBO.GetNlsCultureEntity(businessEntity.BeeEntity.NlsCultureGuid);

            AppDocumentRequestCreate appDocumentRequestCreate = new AppDocumentRequestCreate();
            appDocumentRequestCreate.TemplateName = appDocumentTemplateRequest.TemplateName;
            appDocumentRequestCreate.CultureName = cultureEntity.CultureName;

            appDocumentRequestCreate.SchemaPrimaryKeyGuid = GetSchemaPrimaryKey(appDocumentTemplateRequest, utlSchemaTypeEntity);

            using (System.Transactions.TransactionScope ts = _TransactionScopeProvider.GetTransactionScope())
            {

                AppDocumentRequestViewModel appDocumentRequestViewModel = _AppDocumentRequestBO.CreateAppDocumentRequestEntity(appDocumentRequestCreate);

                AppDocumentRequestRcptCreate appDocumentRequestRcptCreate = new AppDocumentRequestRcptCreate();
                appDocumentRequestRcptCreate.AppDocumentRequestGuid = appDocumentRequestViewModel.AppDocumentRequestGuid.Value;
                appDocumentRequestRcptCreate.CodeIdSendType = CommSendType.To;
                appDocumentRequestRcptCreate.CodeIdProfileType = CommProfileType.BeeBusiness;
                appDocumentRequestRcptCreate.PrimaryKeyGuid = businessEntity.BeeEntityGuid;
                _AppDocumentRequestRcptBO.CreateAppDocumentRequestRcptEntity(appDocumentRequestRcptCreate);


                DocumentRequestMessage documentRequestMessage = new DocumentRequestMessage()
                {
                    AppDocumentRequestGuid = appDocumentRequestViewModel.AppDocumentRequestGuid.Value,
                    GenPlanGuid = appDocumentTemplateRequest.GenPlanGuid
                };

                _MassTransitPublisherBO.PublishDomainMessage(GetType(), documentRequestMessage);


                ts.Complete();
            }

        }

        public async Task Generate(DocumentRequestMessage documentRequestMessage)
        {
            _Logger.MethodStartWithParameters(documentRequestMessage);

            AppDocumentRequestEntity appDocumentRequestEntity = _AppDocumentRequestBO.GetAppDocumentRequestEntity(documentRequestMessage.AppDocumentRequestGuid);
            NlsDocumentTemplateEntity nlsDocumentTemplateEntity = _NlsDocumentTemplateBO.GetNlsDocumentTemplateEntity(appDocumentRequestEntity.AppDocumentTemplateGuid, appDocumentRequestEntity.NlsCultureGuid);

            IFluidTemplate template = _FluidParser.Parse(nlsDocumentTemplateEntity.MessageBody);

            AppDocumentRequestRcptGetList appDocumentRequestRcptGetList = new AppDocumentRequestRcptGetList();
            appDocumentRequestRcptGetList.DatabaseFilter = new FilterContainer()
            {
                Where = new TreeFilter
                {
                    OperatorType = TreeFilterType.And,
                    Operands = new List<TreeFilter>()
                    {
                        new TreeFilter
                        {
                            Field = "AppDocumentRequestGuid",
                            Value = appDocumentRequestEntity.AppDocumentRequestGuid,
                            FilterType = WhereFilterType.Equal,
                        }
                    }
                }
            };

            List<AppDocumentRequestRcptViewModel> appDocumentRequestRcptViewModel = _AppDocumentRequestRcptBO.GetAppDocumentRequestRcptViewModelList(appDocumentRequestRcptGetList);
            List<string> toEmailAddresses = GetToEmailAddresses(appDocumentRequestRcptViewModel);

            Dictionary<string, object> mergeData = GetMergeData(documentRequestMessage.GenPlanGuid, appDocumentRequestEntity, nlsDocumentTemplateEntity);

            foreach (string toEmailAddress in toEmailAddresses)
            {
                var context = new TemplateContext(mergeData);
                string body = template.Render(context);

                await _FluentEmail.To(toEmailAddress)
                    .Subject(nlsDocumentTemplateEntity.DocumentSubject)
                    .Body(body, true).SendAsync();
            }

            _AppDocumentRequestBO.UpdateToGenerated(appDocumentRequestEntity, this.GetType());
        }
        public async Task<string> Generate(NlsDocumentTemplateGenerateMessageBody nlsDocumentTemplateGenerate)
        {
            string aiSystemPromptForDocumentTemplateMessageBody = "You are an html templates assistant using the liquid template language and having output of html. The available tokens are: {0}. For easy of editing the template, use <div> tags with styles  display: table, display:table-row and display: table-cell instead of <table><row> and <col>";
            string aiSystemPromptForDocumentTemplateMessageBodyWithLanguage = "You are an html templates assistant using the liquid template language and having output of html. The available tokens are: {0}. The response should contain only the html template and should be in the localized to {1}. For easy of editing the template, use <div> tags with styles  display: table, display:table-row and display: table-cell instead of <table><row> and <col>";

            string overrideSystemMessage = string.Empty;
            if (_TtgApplicationSettings.AISystemPrompts.TryGetValue("AISystemPromptForDocumentTemplateMessageBody", out overrideSystemMessage)) aiSystemPromptForDocumentTemplateMessageBody = overrideSystemMessage;
            if (_TtgApplicationSettings.AISystemPrompts.TryGetValue("AISystemPromptForDocumentTemplateMessageBodyWithLanguage", out overrideSystemMessage)) aiSystemPromptForDocumentTemplateMessageBodyWithLanguage = overrideSystemMessage;

            AppDocumentTemplateTokenGetListResponse tokenGetListResponse = GetTokenList(new AppDocumentTemplateTokenGetList() { TemplateName = nlsDocumentTemplateGenerate.TemplateName });

            List<string> validTokens = new List<string>(nlsDocumentTemplateGenerate.Tokens);
            validTokens.RemoveAll(token => !tokenGetListResponse.Items.Contains(token));
            string tokens = string.Join(",", validTokens);

            string systemMessage = string.Empty;
            CultureInfo cultureInfo = new CultureInfo(nlsDocumentTemplateGenerate.CultureName);
            if (cultureInfo.Equals(CultureInfo.InvariantCulture) || string.Equals(cultureInfo.Name, "en-US", StringComparison.InvariantCultureIgnoreCase))
            {
                systemMessage = string.Format(aiSystemPromptForDocumentTemplateMessageBody, tokens);
            }
            else
            {
                systemMessage = string.Format(aiSystemPromptForDocumentTemplateMessageBodyWithLanguage, tokens, cultureInfo.Name);
            }

            IAIWrapper aiWrapper = _AIWrapperFactory.Create();

            string response = await aiWrapper.CompleteChatAsync(new string[] { systemMessage }, nlsDocumentTemplateGenerate.UserPrompts );
            string cleanedResponse = aiWrapper.CleanResponse(response);
            return cleanedResponse;
        }

        public AppDocumentTemplateTokenGetListResponse GetTokenList(AppDocumentTemplateTokenGetList appDocumentTemplateTokenGetList)
        {
            AppDocumentTemplateEntity appDocumentTemplateEntity = _AppDocumentTemplateBO.GetAppDocumentTemplateEntity(appDocumentTemplateTokenGetList.TemplateName);
            UtlSchemaEntity utlSchemaEntity = _UtlSchemaBO.GetUtlSchemaEntity(appDocumentTemplateEntity.UtlSchemaGuid.Value);
            UtlSchemaTypeEntity utlSchemaTypeEntity = _UtlSchemaTypeBO.GetUtlSchemaTypeEntity(utlSchemaEntity.UtlSchemaTypeGuid);
            DataTable dataTable = GetDataTable(utlSchemaEntity, utlSchemaTypeEntity, 0m);
            AppDocumentTemplateTokenGetListResponse response = new AppDocumentTemplateTokenGetListResponse() { Items = new List<string>() };
            foreach (DataColumn dataColumn in dataTable.Columns)
            {
                response.Items.Add($"{dataColumn.ColumnName}");
            }
            AddAutomaticMergeFields(response);

            response.Items = response.Items.Distinct().ToList();

            return response;
        }

        private Dictionary<string, object> GetMergeData(decimal genPlanGuid, AppDocumentRequestEntity appDocumentRequestEntity, NlsDocumentTemplateEntity nlsDocumentTemplateEntity)
        {
            Dictionary<string, object> mergeData = new Dictionary<string, object>();

            if (appDocumentRequestEntity.SchemaPrimaryKeyGuid.HasValue)
            {
                AppDocumentTemplateEntity appDocumentTemplateEntity = _AppDocumentTemplateBO.GetAppDocumentTemplateEntity(appDocumentRequestEntity.AppDocumentTemplateGuid);
                DataRow dataRow = GetDataRow(appDocumentRequestEntity, appDocumentTemplateEntity);
                if (dataRow != null)
                {
                    foreach (DataColumn dataColumn in dataRow.Table.Columns)
                    {
                        mergeData.TryAdd(dataColumn.ColumnName, dataRow[dataColumn.ColumnName]);
                        AddAutomaticMergeFields(genPlanGuid, dataColumn.ColumnName, dataRow[dataColumn.ColumnName], mergeData);
                    }
                }
            }

            return mergeData;
        }

        private void AddAutomaticMergeFields(decimal genPlanGuid, string columnName, object value, Dictionary<string, object> mergeData)
        {
            if (columnName.StartsWith("BeeBusinessGuid", StringComparison.InvariantCultureIgnoreCase))
            {
                if (value == DBNull.Value) return;
                string dictionaryKey = columnName.Replace("BeeBusinessGuid", "", StringComparison.InvariantCultureIgnoreCase);
                if (string.IsNullOrWhiteSpace(dictionaryKey)) return;
                AddAutomaticBeeBusinessMergeFields((decimal)value, dictionaryKey, mergeData);
            }
            else if (columnName.StartsWith("AoeOrderId", StringComparison.InvariantCultureIgnoreCase))
            {
                if (value == DBNull.Value) return;
                string dictionaryKey = columnName.Replace("AoeOrderId", "", StringComparison.InvariantCultureIgnoreCase);
                dictionaryKey = dictionaryKey + "Order";
                AddAutomaticOrderMergeFields(genPlanGuid, (long)value, dictionaryKey, mergeData);
            }
            else if (columnName.StartsWith("NlsAddressGuid", StringComparison.InvariantCultureIgnoreCase))
            {
                if (value == DBNull.Value) return;
                string dictionaryKey = columnName.Replace("NlsAddressGuid", "", StringComparison.InvariantCultureIgnoreCase);
                if (string.IsNullOrWhiteSpace(dictionaryKey)) return;
                dictionaryKey = dictionaryKey + "Address";
                AddAutomaticNlsAddressMergeFields((decimal)value, dictionaryKey, mergeData);
            }
        }

        private List<string> GetToEmailAddresses(List<AppDocumentRequestRcptViewModel> appDocumentRequestRcptViewModel)
        {
            List<string> toEmailAddresses = new List<string>();
            foreach (AppDocumentRequestRcptViewModel appDocumentRequestRcpt in appDocumentRequestRcptViewModel)
            {
                if (string.Equals(appDocumentRequestRcpt.SendType.CodeId, CommSendType.To, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (string.Equals(appDocumentRequestRcpt.ProfileType.CodeId, CommProfileType.BeeBusiness, StringComparison.InvariantCultureIgnoreCase))
                    {
                        BeeCommunicationEntity businessEntityCommunicationEntity = _BusinessEntityCommunicationBO.GetBeeCommunicationEntity(appDocumentRequestRcpt.PrimaryKeyGuid.Value);
                        toEmailAddresses.Add(businessEntityCommunicationEntity.EmailAddress);
                    }
                    else
                    {
                        throw new NotSupportedException($"ProfileType.CodeId of '{appDocumentRequestRcpt.ProfileType.CodeId}' not supported");
                    }
                }
            }
            return toEmailAddresses;
        }

        private void AddAutomaticMergeFields(AppDocumentTemplateTokenGetListResponse response)
        {
            List<string> itemsToAdd = new List<string>();
            foreach (string item in response.Items)
            {
                if (item.StartsWith("BeeBusinessGuid", StringComparison.InvariantCultureIgnoreCase))
                {
                    string dictionaryKey = item.Replace("BeeBusinessGuid", "", StringComparison.InvariantCultureIgnoreCase);
                    if (string.IsNullOrWhiteSpace(dictionaryKey))
                    {
                        dictionaryKey = "BusinessEntity";
                    }
                    AddAutomaticBeeBusinessMergeFields(itemsToAdd, dictionaryKey);
                }
                else if (item.StartsWith("AoeOrderId", StringComparison.InvariantCultureIgnoreCase))
                {
                    string dictionaryKey = item.Replace("AoeOrderId", "", StringComparison.InvariantCultureIgnoreCase);
                    dictionaryKey = dictionaryKey + "Order";
                    AddAutomaticOrderMergeFields(itemsToAdd, dictionaryKey);
                }
                else if (item.StartsWith("NlsAddressGuid", StringComparison.InvariantCultureIgnoreCase))
                {
                    string dictionaryKey = item.Replace("NlsAddressGuid", "", StringComparison.InvariantCultureIgnoreCase);
                    dictionaryKey = dictionaryKey + "Address";
                    AddAutomaticNlsAddressMergeFields(itemsToAdd, dictionaryKey);
                }
            }

            response.Items.AddRange(itemsToAdd);
        }

        #region utility methods
        private decimal? GetSchemaPrimaryKey(AppDocumentTemplateRequest appDocumentTemplateRequest, UtlSchemaTypeEntity utlSchemaTypeEntity)
        {
            if (string.Equals(utlSchemaTypeEntity.DataObject, "BeeBusiness", StringComparison.InvariantCultureIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(appDocumentTemplateRequest.SchemaUniqueIdentifier)) throw new ArgumentException("SchemaUniqueIdentifier(BeeNumber)");
                if (decimal.TryParse(appDocumentTemplateRequest.SchemaUniqueIdentifier, out decimal beeNumber))
                {
                    return _BusinessEntityBOCore.GetBeeBusinessEntity(appDocumentTemplateRequest.GenPlanGuid, beeNumber).BeeBusinessGuid;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("SchemaUniqueIdentifier(BeeNumber)", "SchemaUniqueIdentifier must be a number");
                }
            }
            else if (string.Equals(utlSchemaTypeEntity.DataObject, "BeeEntity", StringComparison.InvariantCultureIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(appDocumentTemplateRequest.SchemaUniqueIdentifier)) throw new ArgumentException("SchemaUniqueIdentifier(BeeNumber)");
                if (decimal.TryParse(appDocumentTemplateRequest.SchemaUniqueIdentifier, out decimal beeNumber))
                {
                    return _BusinessEntityBOCore.GetBeeBusinessEntity(appDocumentTemplateRequest.GenPlanGuid, beeNumber).BeeEntityGuid;
                }
                else
                {
                    throw new ArgumentOutOfRangeException("SchemaUniqueIdentifier(BeeNumber)", "SchemaUniqueIdentifier must be a number");
                }
            }
            else if (string.Equals(utlSchemaTypeEntity.DataObject, "Order", StringComparison.InvariantCultureIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(appDocumentTemplateRequest.SchemaUniqueIdentifier)) throw new ArgumentException("SchemaUniqueIdentifier(AOEOrderId)");
                string aoeOrderId = appDocumentTemplateRequest.SchemaUniqueIdentifier;
                return decimal.Parse(aoeOrderId);
            }
            else
            {
                throw new NotSupportedException($"UtlSchemaType of '{utlSchemaTypeEntity.SchemaTypeName}' not supported");
            }

        }
        private DataRow GetDataRow(AppDocumentRequestEntity appDocumentRequestEntity, AppDocumentTemplateEntity appDocumentTemplateEntity)
        {
            DataRow dataRow = null;
            if (appDocumentTemplateEntity.UtlSchemaGuid.HasValue)
            {
                if (!appDocumentRequestEntity.SchemaPrimaryKeyGuid.HasValue) throw new InvalidOperationException($"SchemaPrimaryKeyGuid is required when AppDocumentTemplate has a UtlSchemaGuid. AppDocumentRequestGuid={appDocumentRequestEntity.AppDocumentRequestGuid}");
                UtlSchemaEntity utlSchemaEntity = _UtlSchemaBO.GetUtlSchemaEntity(appDocumentTemplateEntity.UtlSchemaGuid.Value);
                UtlSchemaTypeEntity utlSchemaTypeEntity = _UtlSchemaTypeBO.GetUtlSchemaTypeEntity(utlSchemaEntity.UtlSchemaTypeGuid);
                DataTable dataTable = GetDataTable(utlSchemaEntity, utlSchemaTypeEntity, appDocumentRequestEntity.SchemaPrimaryKeyGuid.Value);
                if (dataTable != null && dataTable.Rows.Count > 0)
                {
                    dataRow = dataTable.Rows[0];
                }
            }
            return dataRow;
        }

        private DataTable GetDataTable(UtlSchemaEntity entity, UtlSchemaTypeEntity utlSchemaTypeEntity, decimal schemaPrimaryKeyValue)
        {
            DataTable dataTable = new DataTable();

            string commandText = entity.SchemaSQL;

            if (!commandText.Contains(" WHERE ")) commandText = commandText + " WHERE ";
            commandText = commandText + GetWhereClause(utlSchemaTypeEntity);

            SqlCommand command = new SqlCommand(commandText, new SqlConnection(_TtgApplicationSettings.ConnectionString("ACM")));
            command.Parameters.Add(new SqlParameter() { ParameterName = "@SchemaPrimaryKey", Value = schemaPrimaryKeyValue, SqlDbType = SqlDbType.Decimal });

            using (System.Data.SqlClient.SqlDataAdapter dataAdapter = new System.Data.SqlClient.SqlDataAdapter(command))
            {
                dataAdapter.Fill(dataTable);
            }
            return dataTable;
        }

        private string GetWhereClause(UtlSchemaTypeEntity utlSchemaTypeEntity)
        {
            if (string.Equals(utlSchemaTypeEntity.DataObject, SupportedSchemaTypeDataObjects.BeeBusiness, StringComparison.InvariantCultureIgnoreCase))
            {
                return $"table1.BeeBusinessGuid = @SchemaPrimaryKey";
            }
            else if (string.Equals(utlSchemaTypeEntity.DataObject, SupportedSchemaTypeDataObjects.BeeEntity, StringComparison.InvariantCultureIgnoreCase))
            {
                return $"table1.BeeEntityGuid = @SchemaPrimaryKey";
            }
            else if (string.Equals(utlSchemaTypeEntity.DataObject, SupportedSchemaTypeDataObjects.Order, StringComparison.InvariantCultureIgnoreCase))
            {
                return $"table1.AoeOrderId = @SchemaPrimaryKey";
            }
            else
            {
                throw new NotSupportedException($"UtlSchemaType of '{utlSchemaTypeEntity.SchemaTypeName}' not supported");
            }
        }

        #endregion

        #region nlsAddressFields
        private void AddAutomaticNlsAddressMergeFields(List<string> itemsToAd, string dictionaryKey)
        {
            itemsToAd.Add($"{dictionaryKey}.FirstName");
            itemsToAd.Add($"{dictionaryKey}.LastName");
            itemsToAd.Add($"{dictionaryKey}.AddressLine1");
            itemsToAd.Add($"{dictionaryKey}.AddressLine2");
            itemsToAd.Add($"{dictionaryKey}.AddressLine3");
            itemsToAd.Add($"{dictionaryKey}.City");
            itemsToAd.Add($"{dictionaryKey}.StateCode");
            itemsToAd.Add($"{dictionaryKey}.PostalCode");
        }

        private void AddAutomaticNlsAddressMergeFields(decimal nlsAddressGuid, string dictionaryKey, Dictionary<string, object> mergeData)
        {
            NlsAddressEntity nlsAddressEntity = _NlsAddressBO.GetNlsAddressEntityWithNlsState(nlsAddressGuid);
            if (nlsAddressEntity == null) return;

            Dictionary<string, object> addressData = new Dictionary<string, object>();
            mergeData.TryAdd(dictionaryKey, addressData);

            addressData.Add("FirstName", nlsAddressEntity.FirstName);
            addressData.Add("LastName", nlsAddressEntity.LastName);
            addressData.Add("AddressLine1", nlsAddressEntity.AddressLine1);
            addressData.Add("AddressLine2", nlsAddressEntity.AddressLine2);
            addressData.Add("AddressLine3", nlsAddressEntity.AddressLine3);
            addressData.Add("City", nlsAddressEntity.City);
            addressData.Add("StateCode", nlsAddressEntity.NlsState.StateCode);
            addressData.Add("PostalCode", nlsAddressEntity.PostalCode);
        }
        #endregion

        #region orderFields
        private void AddAutomaticOrderMergeFields(List<string> itemsToAd, string dictionaryKey)
        {

            itemsToAd.Add($"{dictionaryKey}.OrderId");
            itemsToAd.Add($"{dictionaryKey}.ShippingCost");
            itemsToAd.Add($"{dictionaryKey}.SubTotal");
            itemsToAd.Add($"{dictionaryKey}.Total");
            itemsToAd.Add($"{dictionaryKey}.ShippingMethodName");

            itemsToAd.Add($"{dictionaryKey}.SkuList.LineNumber");
            itemsToAd.Add($"{dictionaryKey}.SkuList.Price");
            itemsToAd.Add($"{dictionaryKey}.SkuList.Quantity");
            itemsToAd.Add($"{dictionaryKey}.SkuList.SkuCode");
            itemsToAd.Add($"{dictionaryKey}.SkuList.TotalDiscountAmount");
        }

        private void AddAutomaticOrderMergeFields(decimal genPlanGuid, long aoeOrderId, string dictionaryKey, Dictionary<string, object> mergeData)
        {
            AOEOrderExViewModel orderViewModel = _AOEOrderBO.GetAOEOrderExViewModel(genPlanGuid, aoeOrderId);
            if (orderViewModel == null) return;

            Dictionary<string, object> orderData = new Dictionary<string, object>();
            mergeData.TryAdd(dictionaryKey, orderData);

            orderData.Add("OrderId", orderViewModel.AOEOrderId);
            orderData.Add("ShippingCost", orderViewModel.ShippingCost);
            orderData.Add("SubTotal", orderViewModel.Subtotal);
            orderData.Add("Total", orderViewModel.Total);
            orderData.Add("ShippingMethodName", orderViewModel.ShippingMethodName);

            List<Dictionary<string, object>> orderSkus = new List<Dictionary<string, object>>();
            foreach (var sku in orderViewModel.Skus)
            {
                Dictionary<string, object> orderSku = new Dictionary<string, object>();
                orderSkus.Add(orderSku);
                orderSku.Add("LineNumber", sku.LineNumber);
                orderSku.Add("Price", sku.Price);
                orderSku.Add("Quantity", sku.Quantity);
                orderSku.Add("SkuCode", sku.SkuCode);
                orderSku.Add("TotalDiscountAmount", sku.TotalDiscountAmount);
            }

            if (orderSkus.Count > 0) orderData.Add($"SkuList", orderSkus);
        }
        #endregion

        #region beeBusinessFields
        private void AddAutomaticBeeBusinessMergeFields(decimal beeBusinessGuid, string dictionaryKey, Dictionary<string, object> mergeData)
        {
            BeeBusinessEntity beeBusinessEntity = _BusinessEntityBOCore.GetBeeBusinessEntityWithBeeEntityEntity(beeBusinessGuid);
            if (beeBusinessEntity == null) return;

            Dictionary<string, object> businessData = new Dictionary<string, object>();
            mergeData.TryAdd(dictionaryKey, businessData);

            businessData.Add("BeeNumber", beeBusinessEntity.BeeNumber);
            businessData.Add("FirstName", beeBusinessEntity.BeeEntity.FirstName);
            businessData.Add("LastName", beeBusinessEntity.BeeEntity.LastName);
            businessData.Add("MiddleName", beeBusinessEntity.BeeEntity.MiddleName);

            BeeCommunicationEntity beeCommunicationEntity = _BusinessEntityCommunicationBO.GetBeeCommunicationEntity(beeBusinessEntity.BeeEntityGuid);
            if (beeBusinessEntity != null)
            {
                businessData.Add("EmailAddress", beeCommunicationEntity.EmailAddress);
            }
        }
        private void AddAutomaticBeeBusinessMergeFields(List<string> itemsToAd, string dictionaryKey)
        {
            itemsToAd.Add($"{dictionaryKey}.BeeNumber");
            itemsToAd.Add($"{dictionaryKey}.FirstName");
            itemsToAd.Add($"{dictionaryKey}.LastName");
            itemsToAd.Add($"{dictionaryKey}.MiddleName");
            itemsToAd.Add($"{dictionaryKey}.EmailAddress");
        }

        #endregion

        private class SupportedSchemaTypeDataObjects
        {
            public const string BeeBusiness = "BeeBusiness";
            public const string BeeEntity = "BeeEntity";
            public const string Order = "Order";
        }
    }

}
