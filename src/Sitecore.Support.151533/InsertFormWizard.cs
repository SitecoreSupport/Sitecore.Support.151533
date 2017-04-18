using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Utility;
using Sitecore.Forms.Shell.UI.Controls;
using Sitecore.Globalization;
using Sitecore.Layouts;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.WFFM.Abstractions.Dependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Sitecore.Support.Forms.Shell.UI
{
  public class InsertFormWizard : CreateFormWizard
  {
    private string currentItemUri;
    protected WizardDialogBaseXmlControl FormName;
    protected Radiobutton InsertForm;
    protected PlaceholderList Placeholders;
    protected WizardDialogBaseXmlControl SelectPlaceholder;

    protected override void ActivePageChanged(string page, string oldPage)
    {
      base.ActivePageChanged(page, oldPage);
      if ((page == "ConfirmationPage") && this.InsertForm.Checked)
      {
        base.CancelButton.Header = "Cancel";
        base.NextButton.Header = "Insert";
      }
    }

    protected override bool ActivePageChanging(string page, ref string newpage)
    {
      bool flag = true;
      if (!base.AnalyticsSettings.IsAnalyticsAvailable && (newpage == "AnalyticsPage"))
      {
        newpage = "ConfirmationPage";
      }
      if (base.CheckGoalSettings(page, ref newpage))
      {
        if ((this.InsertForm.Checked && (page == "CreateForm")) && (newpage == "FormName"))
        {
          newpage = "SelectForm";
        }
        if ((this.InsertForm.Checked && (page == "ConfirmationPage")) && (newpage == "AnalyticsPage"))
        {
          newpage = "SelectPlaceholder";
        }
        if ((this.InsertForm.Checked && (page == "SelectForm")) && (newpage == "FormName"))
        {
          newpage = "CreateForm";
        }
        if (((page == "CreateForm") || (page == "FormName")) && (newpage == "SelectForm"))
        {
          if (base.EbFormName.Value == string.Empty)
          {
            Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("EMPTY_FORM_NAME"));
            newpage = (page == "CreateForm") ? "CreateForm" : "FormName";
            return flag;
          }
          if (this.FormsRoot.Database.GetItem(this.FormsRoot.Paths.ContentPath + "/" + base.EbFormName.Value) != null)
          {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("'{0}' ", base.EbFormName.Value);
            builder.Append(DependenciesManager.ResourceManager.Localize("IS_NOT_UNIQUE_NAME"));
            Context.ClientPage.ClientResponse.Alert(builder.ToString());
            newpage = (page == "CreateForm") ? "CreateForm" : "FormName";
            return flag;
          }
          if (!Regex.IsMatch(base.EbFormName.Value, Sitecore.Configuration.Settings.ItemNameValidation, RegexOptions.ECMAScript))
          {
            StringBuilder builder2 = new StringBuilder();
            builder2.AppendFormat("'{0}' ", base.EbFormName.Value);
            builder2.Append(DependenciesManager.ResourceManager.Localize("IS_NOT_VALID_NAME"));
            Context.ClientPage.ClientResponse.Alert(builder2.ToString());
            newpage = (page == "CreateForm") ? "CreateForm" : "FormName";
            return flag;
          }
          if (base.CreateBlankForm.Checked)
          {
            newpage = !string.IsNullOrEmpty(this.Placeholder) ? "ConfirmationPage" : "SelectPlaceholder";
            if (base.AnalyticsSettings.IsAnalyticsAvailable && (newpage == "ConfirmationPage"))
            {
              newpage = "AnalyticsPage";
            }
          }
        }
        if ((page == "SelectForm") && (((newpage == "SelectPlaceholder") || (newpage == "ConfirmationPage")) || (newpage == "AnalyticsPage")))
        {
          string selected = base.multiTree.Selected;
          Item item = StaticSettings.GlobalFormsRoot.Database.GetItem(selected);
          if ((selected == null) || (item == null))
          {
            Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("PLEASE_SELECT_FORM"));
            newpage = "SelectForm";
            return flag;
          }
          if (item.TemplateID != IDs.FormTemplateID)
          {
            StringBuilder builder3 = new StringBuilder();
            builder3.AppendFormat("'{0}' ", item.Name);
            builder3.Append(DependenciesManager.ResourceManager.Localize("IS_NOT_FORM"));
            Context.ClientPage.ClientResponse.Alert(builder3.ToString());
            newpage = "SelectForm";
            return flag;
          }
        }
        if ((newpage == "SelectPlaceholder") && (page == "AnalyticsPage"))
        {
          newpage = string.IsNullOrEmpty(this.Placeholder) ? "SelectPlaceholder" : "SelectForm";
        }
        if (((newpage == "SelectPlaceholder") && (page == "SelectForm")) && !this.InsertForm.Checked)
        {
          newpage = string.IsNullOrEmpty(this.Placeholder) ? "SelectPlaceholder" : (!base.AnalyticsSettings.IsAnalyticsAvailable ? "ConfirmationPage" : "AnalyticsPage");
        }
        if (((newpage == "SelectPlaceholder") && (page == "SelectForm")) && this.InsertForm.Checked)
        {
          newpage = string.IsNullOrEmpty(this.Placeholder) ? "SelectPlaceholder" : "ConfirmationPage";
        }
        if (((page == "ConfirmationPage") && (newpage == "ConfirmationPage")) && !base.AnalyticsSettings.IsAnalyticsAvailable)
        {
          newpage = string.IsNullOrEmpty(this.Placeholder) ? "SelectPlaceholder" : "SelectForm";
        }
        if ((page == "ConfirmationPage") && ((newpage == "SelectPlaceholder") || (newpage == "AnalyticsPage")))
        {
          if (newpage != "AnalyticsPage")
          {
            newpage = string.IsNullOrEmpty(this.Placeholder) ? "SelectPlaceholder" : "SelectForm";
          }
          base.NextButton.Disabled = false;
          base.BackButton.Disabled = false;
          base.CancelButton.Header = "Cancel";
          base.NextButton.Header = "Next >";
        }
        if ((page == "SelectPlaceholder") && ((newpage == "ConfirmationPage") || (newpage == "AnalyticsPage")))
        {
          if (string.IsNullOrEmpty(this.ListValue))
          {
            newpage = "SelectPlaceholder";
            Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("SELECT_MUST_SELECT_PLACEHOLDER"));
          }
          if (this.InsertForm.Checked)
          {
            newpage = "ConfirmationPage";
          }
        }
        if (((((page == "ConfirmationPage") || (page == "AnalyticsPage")) && (newpage == "SelectForm")) || ((page == "SelectPlaceholder") && (newpage == "SelectForm"))) && base.CreateBlankForm.Checked)
        {
          newpage = "CreateForm";
        }
        if (newpage == "ConfirmationPage")
        {
          base.ChoicesLiteral.Text = this.RenderSetting();
        }
      }
      return flag;
    }

    protected override string GenerateItemSetting()
    {
      string str = this.ListValue ?? this.Placeholder;
      string str2 = base.EbFormName.Value;
      Item item = Database.GetItem(ItemUri.Parse(this.currentItemUri));
      StringBuilder builder = new StringBuilder();
      builder.Append("<p>");
      Item formsRootItemForItem = SiteUtils.GetFormsRootItemForItem(item);
      builder.AppendFormat(DependenciesManager.ResourceManager.Localize("FORM_ADDED_MESSAGE"), new object[] { item.Name, str, formsRootItemForItem.Paths.FullPath, str2 });
      builder.Append("</p>");
      return builder.ToString();
    }

    public Item GetCurrentItem()
    {
      string queryString = Sitecore.Web.WebUtil.GetQueryString("id");
      string name = Sitecore.Web.WebUtil.GetQueryString("la");
      string str3 = Sitecore.Web.WebUtil.GetQueryString("vs");
      string databaseName = Sitecore.Web.WebUtil.GetQueryString("db");
      ItemUri uri = new ItemUri(queryString, Language.Parse(name), Sitecore.Data.Version.Parse(str3), databaseName);
      return Database.GetItem(uri);
    }

    protected override void Localize()
    {
      base.Localize();
      this.SelectPlaceholder["Header"] = DependenciesManager.ResourceManager.Localize("SELECT_PLACEHOLDER");
      this.SelectPlaceholder["Text"] = DependenciesManager.ResourceManager.Localize("FORM_WILL_BE_INSERTED_INTO_PLACEHOLDER");
      this.InsertForm.Header = DependenciesManager.ResourceManager.Localize("INSERT_FORM");
      base.CreateForm["Header"] = DependenciesManager.ResourceManager.Localize("INSERT_FORM_HEADER");
      base.CreateForm["Text"] = DependenciesManager.ResourceManager.Localize("INSERT_FORM_TEXT");
      this.FormName["Header"] = DependenciesManager.ResourceManager.Localize("ENTER_FORM_NAME_HEADER");
      this.FormName["Text"] = DependenciesManager.ResourceManager.Localize("ENTER_FORM_NAME_TEXT");
    }

    protected override void OnLoad(EventArgs e)
    {
      if (!Context.ClientPage.IsEvent)
      {
        Item currentItem = this.GetCurrentItem();
        this.currentItemUri = currentItem.Uri.ToString();
        this.Localize();
      }
      base.OnLoad(e);
      if (!Context.ClientPage.IsEvent)
      {
        Item item2 = this.GetCurrentItem();
        base.EbFormName.Value = base.GetUniqueName(item2.Name);
        this.Layout = item2[Sitecore.FieldIDs.LayoutField];
        this.Placeholders.DeviceID = this.DeviceID;
        this.Placeholders.ShowDeviceTree = string.IsNullOrEmpty(this.Mode);
        this.Placeholders.ItemUri = this.currentItemUri;
        this.Placeholders.AllowedRendering = StaticSettings.GetRendering(item2).ToString();
      }
      else
      {
        this.currentItemUri = base.ServerProperties["forms_current_item"] as string;
      }
    }

    protected override void OnNext(object sender, EventArgs formEventArgs)
    {
      if ((base.NextButton.Header == "Create") || (base.NextButton.Header == "Insert"))
      {
        this.SaveForm();
        SheerResponse.SetModified(false);
      }
      base.Next();
    }

    protected override void OnPreRender(EventArgs e)
    {
      base.OnPreRender(e);
      base.ServerProperties["forms_current_item"] = this.currentItemUri;
    }

    protected override void SaveForm()
    {
      Item form;
      string deviceID = this.Placeholders.DeviceID;
      if (!this.InsertForm.Checked)
      {
        base.SaveForm();
        form = Database.GetItem(ItemUri.Parse((string)base.ServerProperties[base.newFormUri]));
      }
      else
      {
        string queryString = Sitecore.Web.WebUtil.GetQueryString("la");
        Language contentLanguage = Context.ContentLanguage;
        if (!string.IsNullOrEmpty(queryString))
        {
          Language.TryParse(Sitecore.Web.WebUtil.GetQueryString("la"), out contentLanguage);
        }
        form = this.FormsRoot.Database.GetItem(base.CreateBlankForm.Checked ? string.Empty : base.multiTree.Selected, contentLanguage);
      }
      if ((this.Mode != StaticSettings.DesignMode) && (this.Mode != "edit"))
      {
        Item item = Database.GetItem(ItemUri.Parse(this.currentItemUri));
        LayoutDefinition definition = LayoutDefinition.Parse(LayoutField.GetFieldValue(item.Fields[Sitecore.FieldIDs.LayoutField]));
        RenderingDefinition rendering = new RenderingDefinition();
        string listValue = this.ListValue;
        ID id = StaticSettings.GetRendering(definition);
        rendering.ItemID = id.ToString();
        if (rendering.ItemID == IDs.FormInterpreterID.ToString())
        {
          rendering.Parameters = "FormID=" + form.ID;
        }
        else
        {
          rendering.Datasource = form.ID.ToString();
        }
        rendering.Placeholder = listValue;
        DeviceDefinition device = definition.GetDevice(deviceID);
        List<RenderingDefinition> renderings = device.GetRenderings(rendering.ItemID);
        if ((id != IDs.FormMvcInterpreterID) && renderings.Any<RenderingDefinition>(x => (((x.Parameters != null) && x.Parameters.Contains(rendering.Parameters)) || ((x.Datasource != null) && x.Datasource.Contains(form.ID.ToString())))))
        {
          Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("FORM_CANT_BE_INSERTED"));
        }
        else
        {
          item.Editing.BeginEdit();
          device.AddRendering(rendering);
          if (item.Name != "__Standard Values")
          {
            LayoutField.SetFieldValue(item.Fields[Sitecore.FieldIDs.LayoutField], definition.ToXml());
          }
          else
          {
            item[Sitecore.FieldIDs.LayoutField] = definition.ToXml();
          }
          item.Editing.EndEdit();
        }
      }
      else
      {
        LayoutDefinition definition3 = LayoutDefinition.Parse(LayoutField.GetFieldValue(Database.GetItem(ItemUri.Parse(this.currentItemUri)).Fields[Sitecore.FieldIDs.LayoutField]));
        RenderingDefinition rendering = new RenderingDefinition();
        string str4 = this.ListValue;
        ID id2 = StaticSettings.GetRendering(definition3);
        rendering.ItemID = id2.ToString();
        rendering.Parameters = "FormID=" + form.ID;
        rendering.Datasource = form.ID.ToString();
        rendering.Placeholder = str4;
        List<RenderingDefinition> source = definition3.GetDevice(deviceID).GetRenderings(rendering.ItemID);
        if ((id2 != IDs.FormMvcInterpreterID) && source.Any<RenderingDefinition>(x => (((x.Parameters != null) && x.Parameters.Contains(rendering.Parameters)) || ((x.Datasource != null) && x.Datasource.Contains(form.ID.ToString())))))
        {
          Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("FORM_CANT_BE_INSERTED"));
        }
        else
        {
          SheerResponse.SetDialogValue(form.ID.ToString());
        }
      }
    }

    public string DeviceID =>
        Sitecore.Web.WebUtil.GetQueryString("deviceid");

    protected override Item FormsRoot =>
        SiteUtils.GetFormsRootItemForItem(Database.GetItem(ItemUri.Parse(this.currentItemUri)));

    public bool IsCalledFromPageEditor =>
        (Sitecore.Web.WebUtil.GetQueryString("pe", "0") == "1");

    public string Layout
    {
      get
      {
        return StringUtil.GetString(base.ServerProperties["LayoutCurrent"]);
      }
      set
      {
        Assert.ArgumentNotNull(value, "value");
        base.ServerProperties["LayoutCurrent"] = value;
      }
    }

    public string ListValue =>
        this.Placeholders.SelectedPlaceholder;

    public string Mode =>
        Sitecore.Web.WebUtil.GetQueryString("mode");

    public string Placeholder =>
        Sitecore.Web.WebUtil.GetQueryString("placeholder");

    protected override bool RenderConfirmationFormSection =>
        !this.InsertForm.Checked;
  }
}
