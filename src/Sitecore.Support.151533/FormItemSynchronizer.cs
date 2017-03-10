using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Data;
using Sitecore.Form.Core.Utility;
using Sitecore.Forms.Core.Data;
using Sitecore.Globalization;
using System.Linq;
namespace Sitecore.Support.Forms.Core.Data
{
    internal class FormItemSynchronizer
    {
        private readonly Database database;
        private readonly FormDefinition definition;
        private Item formItem;
        private readonly Language language;

        public FormItemSynchronizer(Database database, Language language, FormDefinition definition)
        {
            Assert.ArgumentNotNull(database, "database");
            Assert.ArgumentNotNull(language, "language");
            Assert.ArgumentNotNull(definition, "definition");
            this.database = database;
            this.language = language;
            this.definition = definition;
        }

        protected bool DeleteFieldIsEmpty(FieldDefinition field)
        {
            if (field != null)
            {
                Item item = this.database.GetItem(field.FieldID, this.language);
                if (field.Deleted == "1")
                {
                    if (item != null)
                    {
                        item.Delete();
                    }
                    return true;
                }
                if (string.IsNullOrEmpty(field.Name))
                {
                    field.Deleted = "1";
                    if (item != null)
                    {
                        Sitecore.Form.Core.Utility.Utils.RemoveVersionOrItem(item);
                    }
                    return true;
                }
            }
            return false;
        }

        protected bool DeleteSectionIsEmpty(SectionDefinition section)
        {
            if (section != null)
            {
                bool flag = section.Deleted == "1";
                if (string.IsNullOrEmpty(section.Name))
                {
                    if (section.IsHasOnlyEmptyField)
                    {
                        section.Deleted = "1";
                    }
                    else
                    {
                        section.Name = string.Empty;
                    }
                }
                if (section.Deleted == "1")
                {
                    Item item = this.database.GetItem(section.SectionID, this.language);
                    if (item != null)
                    {
                        if (flag)
                        {
                            item.Delete();
                        }
                        else
                        {
                            Sitecore.Form.Core.Utility.Utils.RemoveVersionOrItem(item);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public static ID FindMatch(ID oldID, FormItem oldForm, FormItem newForm)
        {
            Assert.ArgumentNotNull(oldID, "oldID");
            Assert.ArgumentNotNull(oldForm, "oldForm");
            Assert.ArgumentNotNull(newForm, "newForm");
            Item item = oldForm.Database.GetItem(oldID);
            if ((item != null) && item.Paths.LongID.Contains(oldForm.ID.ToString()))
            {
                int index = -1;
                if (item.ParentID == oldForm.ID)
                {
                    index = oldForm.InnerItem.Children.IndexOf(item);
                    if ((index > -1) && (newForm.InnerItem.Children.Count<Item>() > index))
                    {
                        return newForm.InnerItem.Children[index].ID;
                    }
                }
                if (item.Parent.ParentID == oldForm.ID)
                {
                    index = oldForm.InnerItem.Children.IndexOf(item.Parent);
                    int num2 = item.Parent.Children.IndexOf(item);
                    if (((index > -1) && (num2 > -1)) && ((newForm.InnerItem.Children.Count<Item>() > index) && (newForm.InnerItem.Children[index].Children.Count<Item>() > num2)))
                    {
                        return newForm.InnerItem.Children[index].Children[num2].ID;
                    }
                }
            }
            return ID.Null;
        }

        public void Synchronize()
        {
            foreach (SectionDefinition definition in this.definition.Sections)
            {
                Item sectionItem = null;
                if (!this.DeleteSectionIsEmpty(definition))
                {
                    sectionItem = this.UpdateSection(definition);
                }
                else if (!string.IsNullOrEmpty(definition.SectionID))
                {
                    sectionItem = definition.UpdateSharedFields(this.database, null);
                }
                foreach (FieldDefinition definition2 in definition.Fields)
                {
                    this.SynchronizeField(sectionItem, definition2);
                }
                if ((sectionItem != null) && !sectionItem.HasChildren)
                {
                    sectionItem.Delete();
                }
            }
        }

        private void SynchronizeField(Item sectionItem, FieldDefinition field)
        {
            if (!this.DeleteFieldIsEmpty(field))
            {
                this.UpdateField(field, sectionItem);
            }
            else
            {
                field.UpdateSharedFields(sectionItem, null, this.database);
            }
        }

        protected void UpdateField(FieldDefinition field, Item sectionItem)
        {
            Assert.ArgumentNotNull(field, "field");
            field.CreateCorrespondingItem(sectionItem ?? this.Form, this.language);
        }

        public static void UpdateIDReferences(FormItem oldForm, FormItem newForm)
        {
            Assert.ArgumentNotNull(oldForm, "oldForm");
            Assert.ArgumentNotNull(newForm, "newForm");
            newForm.SaveActions = UpdateIDs(newForm.SaveActions, oldForm, newForm);
            newForm.CheckActions = UpdateIDs(newForm.CheckActions, oldForm, newForm);
        }

        private static string UpdateIDs(string text, FormItem oldForm, FormItem newForm)
        {
            string str = text;
            if (!string.IsNullOrEmpty(str))
            {
                foreach (ID id in IDUtil.GetIDs(str))
                {
                    ID id2 = FindMatch(id, oldForm, newForm);
                    if (!ID.IsNullOrEmpty(id2))
                    {
                        str = str.Replace(id.ToString(), id2.ToString());
                    }
                }
            }
            return str;
        }

        protected Item UpdateSection(SectionDefinition section)
        {
            if (((section == null) || (this.Form == null)) || (string.IsNullOrEmpty(section.SectionID) && !this.definition.IsHasVisibleSection()))
            {
                return null;
            }
            return section.CreateCorrespondingItem(this.Form, this.language);
        }

        public Item Form
        {
            get
            {
                if ((this.formItem == null) && !string.IsNullOrEmpty(this.definition.FormID))
                {
                    this.formItem = this.database.GetItem(this.definition.FormID, this.language);
                }
                return this.formItem;
            }
        }
    }
}
