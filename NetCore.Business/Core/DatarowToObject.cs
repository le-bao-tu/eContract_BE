using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;

namespace NetCore.Business
{
    public static class DatarowToObject
    {
        public static T ToObject<T>(this DataRow dataRow)
       where T : new()
        {
            T item = new T();
            foreach (DataColumn column in dataRow.Table.Columns)
            {
                PropertyInfo property = item.GetType().GetProperty(column.ColumnName);

                if (property != null && dataRow[column] != DBNull.Value && dataRow[column] != null)
                {
                    object result = Convert.ChangeType(dataRow[column], property.PropertyType);
                    property.SetValue(item, result, null);
                }
            }

            return item;
        }
    }
}
