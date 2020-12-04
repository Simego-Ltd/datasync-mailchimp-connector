using Newtonsoft.Json;
using Simego.DataSync.Engine;
using Simego.DataSync.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Simego.DataSync.Providers.MailChimp
{
    public class MailChimpMemberDatasourceWriter : DataWriterProviderBase
    {
        private MailChimpMemberDatasourceReader DataSourceReader { get; set; }
        private DataSchemaMapping Mapping { get; set; }
        private Dictionary<string, MailChimpDataSchemaItem> MailChimpDataSchema { get; set; }
        private HttpWebRequestHelper WebRequestHelper { get; set; }
        private string ListServiceUrl { get; set; }

        public override void AddItems(List<DataCompareItem> items, IDataSynchronizationStatus status)
        {
            if (items != null && items.Count > 0)
            {
                int currentItem = 0;

                foreach (var item in items)
                {
                    if (!status.ContinueProcessing)
                        break;

                    var itemInvariant = new DataCompareItemInvariant(item);

                    try
                    {                        
                        //Call the Automation BeforeAddItem (Optional only required if your supporting Automation Item Events)
                        Automation?.BeforeAddItem(this, itemInvariant, null);

                        if (itemInvariant.Sync)
                        {
                            #region Add Item

                            //Get the Target Item Data
                            var targetItem = AddItemToDictionary(Mapping, itemInvariant);
                            var targetItemToSend = new Dictionary<string, object>();
                            
                            foreach (var k in targetItem.Keys)
                            {
                                if (!MailChimpDataSchema.ContainsKey(k))
                                    continue;

                                var mc = MailChimpDataSchema[k];

                                if (mc.ReadOnly)
                                    continue;

                                if (mc.IsSubValue)
                                {
                                    if (mc.IsArray)
                                    {
                                        if (mc.ObjectName == "tags")
                                        {
                                            // If Tags - create an array of Tags to add.
                                            targetItemToSend[mc.ObjectName] = DataSchemaTypeConverter.ConvertTo<string[]>(targetItem[k]);
                                        }
                                    }
                                    else
                                    {
                                        if (!targetItemToSend.ContainsKey(mc.ObjectName))
                                        {
                                            targetItemToSend[mc.ObjectName] = new Dictionary<string, object>();
                                        }

                                        var subValue = targetItemToSend[mc.ObjectName] as Dictionary<string, object>;

                                        subValue[mc.FieldName] = targetItem[k];
                                    }
                                }
                                else
                                {
                                    targetItemToSend[mc.FieldName] = targetItem[k];
                                }                                
                            }                            

                            var json = JsonConvert.SerializeObject(targetItemToSend, Formatting.None);
                            var result = WebRequestHelper.PostRequestAsJson(json, $"{ListServiceUrl}/{DataSourceReader.ListId}/members");
                            var item_id = result["id"].ToObject<string>();
                                                        
                            //Call the Automation AfterAddItem (pass the created item identifier if possible)
                            Automation?.AfterAddItem(this, itemInvariant, item_id);

                        }

                        #endregion

                        ClearSyncStatus(item); //Clear the Sync Flag on Processed Rows

                    }
                    catch (WebException e)
                    {
                        Automation?.ErrorItem(this, itemInvariant, null, e);
                        HandleError(status, e);
                    }
                    catch (SystemException e)
                    {
                        Automation?.ErrorItem(this, itemInvariant, null, e);
                        HandleError(status, e);
                    }
                    finally
                    {
                        status.Progress(items.Count, ++currentItem); //Update the Sync Progress
                    }

                }
            }
        }

        public override void UpdateItems(List<DataCompareItem> items, IDataSynchronizationStatus status)
        {
            if (items != null && items.Count > 0)
            {
                int currentItem = 0;

                foreach (var item in items)
                {
                    if (!status.ContinueProcessing)
                        break;

                    var itemInvariant = new DataCompareItemInvariant(item);

                    // Get the item ID from the Target Identifier Store 
                    var item_id = itemInvariant.GetTargetIdentifier<string>();

                    try
                    {
                       
                        //Call the Automation BeforeUpdateItem (Optional only required if your supporting Automation Item Events)
                        Automation?.BeforeUpdateItem(this, itemInvariant, item_id);

                        if (itemInvariant.Sync)
                        {
                            #region Update Item

                            //Get the Target Item Data
                            var targetItem = UpdateItemToDictionary(Mapping, itemInvariant);
                            var targetItemToSend = new Dictionary<string, object>();

                            foreach (var k in targetItem.Keys)
                            {
                                if (!MailChimpDataSchema.ContainsKey(k))
                                    continue;

                                var mc = MailChimpDataSchema[k];

                                if (mc.ReadOnly)
                                    continue;

                                if (mc.IsSubValue)
                                {                                    
                                    if (mc.IsArray)
                                    {
                                        if (mc.ObjectName == "tags")
                                        {
                                            // If Tags - get the original list of tags and work out which ones to add/remove                                            
                                            
                                            var source_item = itemInvariant.Row.First(p => Mapping.MapColumnToDestination(p) == k);

                                            var existing = GetHashSet<string>(source_item.BeforeColumnValue);
                                            var toadd = GetHashSet<string>(source_item.AfterColumnValue);

                                            var arrayOfDictionary = new List<Dictionary<string, object>>();

                                            // Tags to Add
                                            foreach (var v in toadd.Except(existing))
                                            {
                                                arrayOfDictionary.Add(new Dictionary<string, object>() { { "name", v }, { "status", "active" } });
                                            }

                                            // Tags to Remove
                                            foreach(var v in existing.Except(toadd))
                                            {
                                                arrayOfDictionary.Add(new Dictionary<string, object>() { { "name", v }, { "status", "inactive" } });
                                            }                                            

                                            var tags_json = JsonConvert.SerializeObject(new { tags = arrayOfDictionary }, Formatting.None);
                                            var tags_result = WebRequestHelper.PostRequestAsJson(tags_json, $"{ListServiceUrl}/{DataSourceReader.ListId}/members/{item_id}/tags");
                                        }
                                    }
                                    else
                                    {
                                        if (!targetItemToSend.ContainsKey(mc.ObjectName))
                                        {
                                            targetItemToSend[mc.ObjectName] = new Dictionary<string, object>();
                                        }

                                        var subValue = targetItemToSend[mc.ObjectName] as Dictionary<string, object>;

                                        subValue[mc.FieldName] = targetItem[k];
                                    }
                                }
                                else
                                {
                                    targetItemToSend[mc.FieldName] = targetItem[k];
                                }
                            }

                            if (targetItemToSend.Any())
                            {
                                var json = JsonConvert.SerializeObject(targetItemToSend, Formatting.None);
                                var result = WebRequestHelper.PutRequestAsJson(json, $"{ListServiceUrl}/{DataSourceReader.ListId}/members/{item_id}");
                            }


                            //Call the Automation AfterUpdateItem 
                            Automation?.AfterUpdateItem(this, itemInvariant, item_id);


                            #endregion
                        }

                        ClearSyncStatus(item); //Clear the Sync Flag on Processed Rows
                    }
                    catch (WebException e)
                    {
                        Automation?.ErrorItem(this, itemInvariant, item_id, e);
                        HandleError(status, e);
                    }
                    catch (SystemException e)
                    {
                        Automation?.ErrorItem(this, itemInvariant, item_id, e);
                        HandleError(status, e);
                    }
                    finally
                    {
                        status.Progress(items.Count, ++currentItem); //Update the Sync Progress
                    }

                }
            }
        }

        public override void DeleteItems(List<DataCompareItem> items, IDataSynchronizationStatus status)
        {
            if (items != null && items.Count > 0)
            {
                int currentItem = 0;

                foreach (var item in items)
                {
                    if (!status.ContinueProcessing)
                        break;

                    var itemInvariant = new DataCompareItemInvariant(item);

                    // Get the item ID from the Target Identifier Store 
                    var item_id = itemInvariant.GetTargetIdentifier<string>();

                    try
                    {
                        
                        //Call the Automation BeforeDeleteItem (Optional only required if your supporting Automation Item Events)
                        Automation?.BeforeDeleteItem(this, itemInvariant, item_id);

                        if (itemInvariant.Sync)
                        {
                            #region Delete Item

                            var result = WebRequestHelper.DeleteRequestAsJson(null, $"{ListServiceUrl}/{DataSourceReader.ListId}/members/{item_id}");

                            #endregion

                            //Call the Automation AfterDeleteItem 
                            Automation?.AfterDeleteItem(this, itemInvariant, item_id);
                        }

                        ClearSyncStatus(item); //Clear the Sync Flag on Processed Rows
                    }
                    catch (WebException e)
                    {
                        Automation?.ErrorItem(this, itemInvariant, item_id, e);
                        HandleError(status, e);
                    }
                    catch (SystemException e)
                    {
                        Automation?.ErrorItem(this, itemInvariant, item_id, e);
                        HandleError(status, e);
                    }
                    finally
                    {
                        status.Progress(items.Count, ++currentItem); //Update the Sync Progress
                    }

                }
            }
        }

        public override void Execute(List<DataCompareItem> addItems, List<DataCompareItem> updateItems, List<DataCompareItem> deleteItems, IDataSourceReader reader, IDataSynchronizationStatus status)
        {
            DataSourceReader = reader as MailChimpMemberDatasourceReader;

            if (DataSourceReader != null)
            {
                Mapping = new DataSchemaMapping(SchemaMap, DataCompare);

                MailChimpDataSchema = MailChimp.MailChimpDataSchema.MailChimpMemberSchema();
                WebRequestHelper = DataSourceReader.GetWebRequestHelper();
                ListServiceUrl = DataSourceReader.GetUriHelper().ListServiceUrl;

                //Process the Changed Items
                if (addItems != null && status.ContinueProcessing) AddItems(addItems, status);
                if (updateItems != null && status.ContinueProcessing) UpdateItems(updateItems, status);
                if (deleteItems != null && status.ContinueProcessing) DeleteItems(deleteItems, status);

            }
        }

        private static void HandleError(IDataSynchronizationStatus status, Exception e)
        {
            if (!status.FailOnError)
            {
                status.LogMessage(e.Message);
            }
            if (status.FailOnError)
            {
                throw e;
            }
        }
        private void HandleError(IDataSynchronizationStatus status, WebException e)
        {
            if (status.FailOnError)
            {
                throw e;
            }

            if (e.Response != null)
            {
                using (var response = e.Response.GetResponseStream())
                {
                    if (response != null)
                        using (var sr = new StreamReader(response))
                        {
                            string result = sr.ReadToEnd();
                            if (!string.IsNullOrEmpty(result))
                            {
                                status.LogMessage(string.Concat(e.Message, Environment.NewLine, result));
                            }
                        }
                }
            }
            else
            {
                if (!status.FailOnError)
                {
                    status.LogMessage(e.Message);
                }
            }
        }
        
        private HashSet<T> GetHashSet<T>(object value)
        {
            var result = DataSchemaTypeConverter.ConvertTo<T[]>(value);
            return result != null ? new HashSet<T>(result) : new HashSet<T>();
        }
    }
}
