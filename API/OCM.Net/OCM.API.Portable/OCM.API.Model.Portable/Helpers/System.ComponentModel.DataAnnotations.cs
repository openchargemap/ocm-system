using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.ComponentModel.DataAnnotations
{

    public enum DataType
    {
        Custom,
        DateTime,
        Date,
        Time,
        Duration,
        PhoneNumber,
        Currency,
        Text,
        Html,
        MultilineText,
        EmailAddress,
        Password,
        Url,
        ImageUrl,
        CreditCard,
        PostalCode,
        Upload,
    }




    public abstract class ValidationAttribute : Attribute
    {
        private object nestedCallLock = new object();
        private const string DEFAULT_ERROR_MESSAGE = "The field {0} is invalid.";
        private bool nestedCall;
        private string errorMessage;
        private string fallbackErrorMessage;
        private Func<string> errorMessageAccessor;

        public string ErrorMessage
        {
            get
            {
                return this.errorMessage;
            }
            set
            {
                this.errorMessage = value;
                if (this.errorMessage == null)
                    return;
                this.errorMessageAccessor = (Func<string>)null;
            }
        }

        public string ErrorMessageResourceName { get; set; }

        public Type ErrorMessageResourceType { get; set; }

        protected string ErrorMessageString
        {
            get
            {
                return "Error";
            }
        }

        public virtual bool RequiresValidationContext
        {
            get
            {
                return false;
            }
        }

        protected ValidationAttribute()
        {
        }

        protected ValidationAttribute(Func<string> errorMessageAccessor)
        {
            this.errorMessageAccessor = errorMessageAccessor;
        }

        protected ValidationAttribute(string errorMessage)
        {
            this.fallbackErrorMessage = errorMessage;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class DataTypeAttribute : ValidationAttribute
    {
        public string CustomDataType { get; private set; }

        public DataType DataType { get; private set; }

        public DisplayFormatAttribute DisplayFormat { get; protected set; }

        public DataTypeAttribute(DataType dataType)
        {
            this.DataType = dataType;
            DisplayFormatAttribute displayFormatAttribute;
            switch (dataType)
            {
                case DataType.Date:
                    displayFormatAttribute = new DisplayFormatAttribute();
                    displayFormatAttribute.ApplyFormatInEditMode = true;
                    displayFormatAttribute.ConvertEmptyStringToNull = true;
                    displayFormatAttribute.DataFormatString = "{0:d}";
                    displayFormatAttribute.HtmlEncode = true;
                    break;
                case DataType.Time:
                    displayFormatAttribute = new DisplayFormatAttribute();
                    displayFormatAttribute.ApplyFormatInEditMode = true;
                    displayFormatAttribute.ConvertEmptyStringToNull = true;
                    displayFormatAttribute.DataFormatString = "{0:t}";
                    displayFormatAttribute.HtmlEncode = true;
                    break;
                case DataType.Currency:
                    displayFormatAttribute = new DisplayFormatAttribute();
                    displayFormatAttribute.ApplyFormatInEditMode = false;
                    displayFormatAttribute.ConvertEmptyStringToNull = true;
                    displayFormatAttribute.DataFormatString = "{0:C}";
                    displayFormatAttribute.HtmlEncode = true;
                    break;
                default:
                    displayFormatAttribute = (DisplayFormatAttribute)null;
                    break;
            }
            this.DisplayFormat = displayFormatAttribute;
        }

        public DataTypeAttribute(string customDataType)
        {
            this.CustomDataType = customDataType;
        }

        public virtual string GetDataTypeName()
        {
            DataType dataType = this.DataType;
            if (dataType == DataType.Custom)
                return this.CustomDataType;
            else
                return ((object)dataType).ToString();
        }

        public bool IsValid(object value)
        {
            return true;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DisplayFormatAttribute : Attribute
    {
        public bool ApplyFormatInEditMode { get; set; }

        public bool ConvertEmptyStringToNull { get; set; }

        public string DataFormatString { get; set; }

        public string NullDisplayText { get; set; }

        public bool HtmlEncode { get; set; }
    }


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class RequiredAttribute : ValidationAttribute
    {
        public bool AllowEmptyStrings { get; set; }

        public bool IsValid(object value)
        {
            if (value == null)
                return false;
            string str = value as string;
            if (str != null && !this.AllowEmptyStrings)
                return str.Length > 0;
            else
                return true;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DisplayNameAttribute : Attribute
    {
        public DisplayNameAttribute(string name)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class RangeAttribute : ValidationAttribute
    {
        public RangeAttribute(int a, int b) { }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class StringLengthAttribute : ValidationAttribute
    {
        public StringLengthAttribute(int len) { }
    }
}
