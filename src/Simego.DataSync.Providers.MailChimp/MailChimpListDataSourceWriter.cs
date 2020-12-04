using Simego.DataSync;
using Simego.DataSync.Engine;
using Simego.DataSync.Interfaces;
using Simego.DataSync.Providers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Simego.DataSync.Providers.MailChimp
{
    public class MailChimpListDataSourceWriter : DataWriterProviderBase
    {
        private MailChimpListDatasourceReader DataSourceReader { get; set; }
        private DataSchemaMapping Mapping { get; set; }

        public override void AddItems(List<DataCompareItem> items, IDataSynchronizationStatus status)
        {
            if (items != null && items.Count > 0)
            {
                int currentItem = 0;
                
                foreach (var item in items.Select(p => new DataCompareItemInvariant(p)))
                {
                    if (!status.ContinueProcessing)
                        break;

                    try
                    {
                        //Call the Automation BeforeAddItem
                        if (Automation != null)
                            Automation.BeforeAddItem(this, item, null);

                        if (item.Sync)
                        {
                            #region Add Item

                            foreach (DataCompareColumnItem dcci in item.SourceRow)
                            {
                                if (!Mapping.ColumnMapsToDestination(dcci))
                                    continue;

                                string columnB = Mapping.MapColumnToDestination(dcci);

                                object sourceValue = dcci.BeforeColumnValue;

                                //Ignore Null Values
                                if (sourceValue == null)
                                    continue;

                                //TODO: Add the Item to the Target

                                //Call the Automation AfterAddItem (pass the created identifier if possible)
                                if (Automation != null)
                                    Automation.AfterAddItem(this, item, null);
                            }

                            #endregion
                        }
                    }
                    catch (Exception)
                    {
                        //TODO: Handle Error
                        
                        if (status.FailOnError)
                            throw;
                    }
                    finally
                    {
                        status.Progress(items.Count, ++currentItem);
                    }

                }
            }
        }

        public override void UpdateItems(List<DataCompareItem> items, IDataSynchronizationStatus status)
        {
            if (items != null && items.Count > 0)
            {
                int currentItem = 0;

                //Pass an array of ints to DataCompareItemInvariant to identify identifier fields to copy
                foreach (var item in items.Select(p => new DataCompareItemInvariant(p)))
                {
                    if (!status.ContinueProcessing)
                        break;

                    try
                    {
                        //Example: Get the item ID from the Target Identifier Store 
                        var item_id = item.GetTargetIdentifier<int>();

                        //Call the Automation BeforeUpdateItem
                        if (Automation != null)
                            Automation.BeforeUpdateItem(this, item, item_id);

                        if (item.Sync)
                        {
                            #region Update Item

                            foreach (DataCompareColumnItem dcci in item.Row)
                            {
                                if (!Mapping.ColumnMapsToDestination(dcci))
                                    continue;

                                string columnB = Mapping.MapColumnToDestination(dcci);

                                object sourceValue = dcci.AfterColumnValue;

                                //TODO: Update the Item in the Target

                                //Call the Automation AfterUpdateItem 
                                if (Automation != null)
                                    Automation.AfterUpdateItem(this, item, item_id);
                            }
                            #endregion
                        }
                    }
                    catch (Exception)
                    {
                        //TODO: Handle Error

                        if (status.FailOnError)
                            throw;
                    }
                    finally
                    {
                        status.Progress(items.Count, ++currentItem);
                    }

                }
            }
        }

        public override void DeleteItems(List<DataCompareItem> items, IDataSynchronizationStatus status)
        {
            if (items != null && items.Count > 0)
            {
                int currentItem = 0;

                //Pass an array of ints to DataCompareItemInvariant to identify identifier fields to copy
                foreach (var item in items.Select(p => new DataCompareItemInvariant(p)))
                {
                    if (!status.ContinueProcessing)
                        break;

                    try
                    {
                        //Example: Get the item ID from the Target Identifier Store 
                        var item_id = item.GetTargetIdentifier<int>();

                        //Call the Automation BeforeDeleteItem
                        if (Automation != null)
                            Automation.BeforeDeleteItem(this, item, item_id);

                        if (item.Sync)
                        {
                            #region Delete Item

                            //TODO: Delete the Item in the Target

                            #endregion

                            //Call the Automation AfterDeleteItem 
                            if (Automation != null)
                                Automation.AfterDeleteItem(this, item, item_id);
                        }
                    }
                    catch (Exception)
                    {
                        //TODO: Handle Error

                        if (status.FailOnError)
                            throw;
                    }
                    finally
                    {
                        status.Progress(items.Count, ++currentItem);
                    }

                }
            }
        }

        public override void Execute(List<DataCompareItem> addItems, List<DataCompareItem> updateItems, List<DataCompareItem> deleteItems, IDataSourceReader reader, IDataSynchronizationStatus status)
        {
            DataSourceReader = reader as MailChimpListDatasourceReader;

            if (DataSourceReader != null)
            {
                Mapping = new DataSchemaMapping(SchemaMap, DataCompare);

                //Process the Changed Items
                if (addItems != null && status.ContinueProcessing) AddItems(addItems, status);
                if (updateItems != null && status.ContinueProcessing) UpdateItems(updateItems, status);
                if (deleteItems != null && status.ContinueProcessing) DeleteItems(deleteItems, status);

            }
        }        
    }
}
