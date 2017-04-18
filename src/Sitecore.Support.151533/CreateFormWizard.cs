using HtmlAgilityPack;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Core.Renderings;
using Sitecore.Form.UI.Controls;
using Sitecore.Forms.Core.Data;
using Sitecore.Forms.Shell.UI.Controls;
using Sitecore.Globalization;
using Sitecore.StringExtensions;
using Sitecore.Support.Forms.Core.Data;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.WFFM.Abstractions.Analytics;
using Sitecore.WFFM.Abstractions.Data;
using Sitecore.WFFM.Abstractions.Dependencies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace Sitecore.Support.Forms.Shell.UI
{
  public class CreateFormWizard : WizardForm
  {
    protected Groupbox AnalyticsOptions;
    protected WizardDialogBaseXmlControl AnalyticsPage;
    protected readonly IAnalyticsSettings AnalyticsSettings = DependenciesManager.Resolve<IAnalyticsSettings>();
    protected Literal ChoicesLiteral;
    protected Radiobutton ChooseForm;
    protected WizardDialogBaseXmlControl ConfirmationPage;
    protected Radiobutton CreateBlankForm;
    protected WizardDialogBaseXmlControl CreateForm;
    protected Radiobutton CreateGoal;
    protected Groupbox DropoutOptions;
    protected Edit EbFormName;
    protected Literal EnableDropoutSavedToLiteral;
    protected Checkbox EnableFormDropoutTracking;
    protected Literal EnableFormDropoutTrackingLiteral;
    protected Scrollbox ExistingForms;
    protected Literal FormNameLiteral;
    private Item formsRoot;
    protected Frame GlobalForms;
    protected Edit GoalName;
    protected Literal GoalNameLiteral;
    protected TreePickerEx Goals;
    protected DataContext GoalsDataContext;
    private Item goalsRoot;
    protected MultiTreeView multiTree;
    protected readonly string multiTreeID = "Forms_MultiTreeView";
    protected readonly string newFormUri = "newFormUriKey";
    protected Checkbox OpenNewForm;
    protected Edit Points;
    protected Literal PointsLiteral;
    protected WizardDialogBaseXmlControl SelectForm;
    protected Radiobutton SelectGoal;
    protected Literal SelectGoalLiteral;

    protected override void ActivePageChanged(string page, string oldPage)
    {
      base.ActivePageChanged(page, oldPage);
      if (page == "ConfirmationPage")
      {
        base.NextButton.Visible = true;
        base.BackButton.Visible = true;
        base.NextButton.Disabled = false;
        base.NextButton.Disabled = false;
        base.CancelButton.Header = "Cancel";
        base.NextButton.Header = "Create";
      }
      if (oldPage == "ConfirmationPage")
      {
        base.NextButton.Disabled = false;
        base.BackButton.Disabled = false;
        base.CancelButton.Header = "Cancel";
        base.NextButton.Header = "Next >";
      }
      if ((oldPage == "CreateForm") && this.AnalyticsSettings.IsAnalyticsAvailable)
      {
        Func<Item, bool> predicate = null;
        string name = "{0} Form Completed".FormatWith(new object[] { this.EbFormName.Value });
        if (this.GoalsDataContext.CurrentItem != null)
        {
          List<Item> source = new List<Item>(this.GoalsDataContext.CurrentItem.Children.ToArray());
          if (predicate == null)
          {
            predicate = s => s.Name == name;
          }
          if (source.Where<Item>(predicate).Count<Item>() > 0)
          {
            int i = 1;
            while (source.FirstOrDefault<Item>(s => (s.Name == "{0} {1} Form Completed".FormatWith(new object[] { this.EbFormName.Value, i }))) != null)
            {
              i++;
            }
            name = "{0} {1} Form Completed".FormatWith(new object[] { this.EbFormName.Value, i });
          }
        }
        this.GoalName.Value = name;
        SheerResponse.SetOuterHtml(this.GoalName.ID, this.GoalName);
      }
    }

    protected override bool ActivePageChanging(string page, ref string newpage)
    {
      bool flag = base.ActivePageChanging(page, ref newpage);
      if (this.CheckGoalSettings(page, ref newpage))
      {
        if (!this.AnalyticsSettings.IsAnalyticsAvailable && (newpage == "AnalyticsPage"))
        {
          newpage = "ConfirmationPage";
        }
        if ((page == "CreateForm") && (newpage == "SelectForm"))
        {
          if (this.EbFormName.Value == string.Empty)
          {
            Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("EMPTY_FORM_NAME"));
            newpage = "CreateForm";
            return flag;
          }
          if (!Regex.IsMatch(this.EbFormName.Value, Sitecore.Configuration.Settings.ItemNameValidation, RegexOptions.ECMAScript))
          {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("'{0}' ", this.EbFormName.Value);
            builder.Append(DependenciesManager.ResourceManager.Localize("IS_NOT_VALID_NAME"));
            Context.ClientPage.ClientResponse.Alert(builder.ToString());
            newpage = "CreateForm";
            return flag;
          }
          if (this.FormsRoot.Database.GetItem(this.FormsRoot.Paths.ContentPath + "/" + this.EbFormName.Value) != null)
          {
            StringBuilder builder2 = new StringBuilder();
            builder2.AppendFormat("'{0}' ", this.EbFormName.Value);
            builder2.Append(DependenciesManager.ResourceManager.Localize("IS_NOT_UNIQUE_NAME"));
            Context.ClientPage.ClientResponse.Alert(builder2.ToString());
            newpage = "CreateForm";
            return flag;
          }
          if (this.CreateBlankForm.Checked)
          {
            newpage = this.AnalyticsSettings.IsAnalyticsAvailable ? "AnalyticsPage" : "ConfirmationPage";
          }
        }
        if ((page == "SelectForm") && ((newpage == "ConfirmationPage") || (newpage == "AnalyticsPage")))
        {
          string selected = this.multiTree.Selected;
          Item item = this.FormsRoot.Database.GetItem(selected);
          if ((selected == null) || (item == null))
          {
            Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("PLEASE_SELECT_FORM"));
            newpage = "SelectForm";
            return flag;
          }
          if (item.TemplateID != Sitecore.Form.Core.Configuration.IDs.FormTemplateID)
          {
            StringBuilder builder3 = new StringBuilder();
            builder3.AppendFormat("'{0}' ", item.Name);
            builder3.Append(DependenciesManager.ResourceManager.Localize("IS_NOT_FORM"));
            Context.ClientPage.ClientResponse.Alert(builder3.ToString());
            newpage = "SelectForm";
            return flag;
          }
        }
        if (((page == "ConfirmationPage") && (newpage == "ConfirmationPage")) && !this.AnalyticsSettings.IsAnalyticsAvailable)
        {
          newpage = this.CreateBlankForm.Checked ? "CreateForm" : "SelectForm";
        }
        if ((page == "ConfirmationPage") && ((newpage == "SelectForm") || (newpage == "AnalyticsPage")))
        {
          base.CancelButton.Header = "Cancel";
          base.NextButton.Header = "Next >";
        }
        if (((page == "ConfirmationPage") || (page == "AnalyticsPage")) && ((newpage == "SelectForm") && this.CreateBlankForm.Checked))
        {
          newpage = "CreateForm";
        }
        if (newpage == "ConfirmationPage")
        {
          this.ChoicesLiteral.Text = this.RenderSetting();
        }
      }
      return flag;
    }

    protected bool CheckGoalSettings(string page, ref string newpage)
    {
      Func<Item, bool> predicate = null;
      if ((page == "AnalyticsPage") && (newpage == "ConfirmationPage"))
      {
        if (!this.CreateGoal.Checked)
        {
          Item item = StaticSettings.ContextDatabase.GetItem(this.Goals.Value);
          if ((item == null) || ((item.TemplateName != "Page Event") && (item.TemplateName != "Goal")))
          {
            Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("CHOOSE_GOAL"));
            newpage = "AnalyticsPage";
            return false;
          }
        }
        else
        {
          if (string.IsNullOrEmpty(this.GoalName.Value))
          {
            Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("ENTER_NAME_FOR_GOAL"));
            newpage = "AnalyticsPage";
            return false;
          }
          List<Item> source = new List<Item>(this.GoalsDataContext.CurrentItem.Children.ToArray());
          if (predicate == null)
          {
            predicate = c => c.Name == this.GoalName.Value;
          }
          if (source.FirstOrDefault<Item>(predicate) != null)
          {
            Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("GOAL_ALREADY_EXISTS", new string[] { this.GoalName.Value }));
            newpage = "AnalyticsPage";
            return false;
          }
          if (!Sitecore.Data.Items.ItemUtil.IsItemNameValid(this.GoalName.Value))
          {
            Context.ClientPage.ClientResponse.Alert(DependenciesManager.ResourceManager.Localize("GOAL_NAME_IS_NOT_VALID", new string[] { this.GoalName.Value }));
            newpage = "AnalyticsPage";
            return false;
          }
        }
      }
      return true;
    }

    protected virtual string GenerateAnalytics()
    {
      StringBuilder builder = new StringBuilder();
      builder.Append("<p>");
      builder.Append("<table>");
      builder.Append("<tr><td class='scwfmOptionName'>");
      builder.Append(DependenciesManager.ResourceManager.Localize("ASOCIATED_GOAL"));
      builder.Append("</td><td class='scwfmOptionValue'>");
      string name = this.GoalName.Value;
      if (!this.CreateGoal.Checked)
      {
        name = this.FormsRoot.Database.GetItem(this.Goals.Value).Name;
      }
      builder.AppendFormat(": {0}", name);
      builder.Append("</td></tr>");
      builder.Append("<tr><td class='scwfmOptionName'>");
      builder.Append(DependenciesManager.ResourceManager.Localize("FORM_DROPOUT_TRACKING"));
      builder.Append("</td><td class='scwfmOptionValue'>");
      builder.AppendFormat(": {0}", this.EnableFormDropoutTracking.Checked ? "Enabled" : "Disabled");
      builder.Append("</td></tr>");
      builder.Append("</table>");
      builder.Append("</p>");
      return builder.ToString();
    }

    protected virtual string GenerateFutherInfo()
    {
      StringBuilder builder = new StringBuilder();
      builder.Append("<p>");
      builder.Append(DependenciesManager.ResourceManager.Localize("MARKETING_INFO"));
      builder.Append("</p>");
      if (this.EnableFormDropoutTracking.Checked)
      {
        builder.Append("<p>");
        builder.Append(DependenciesManager.ResourceManager.Localize("DROPOUT_INFO"));
        builder.Append("</p>");
      }
      return builder.ToString();
    }

    protected virtual string GenerateItemSetting()
    {
      string str = this.EbFormName.Value;
      return string.Join("", new string[] { "<p>", string.Format(DependenciesManager.ResourceManager.Localize("FORM_ADDED_IN_MESSAGE"), this.FormsRoot.Paths.FullPath, str), "</p>" });
    }

    protected virtual string GeneratePreview()
    {
      StringBuilder builder = new StringBuilder();
      if (this.ChooseForm.Checked)
      {
        builder.Append("<p>");
        HtmlTextWriter output = new HtmlTextWriter(new StringWriter());
        Item form = StaticSettings.GlobalFormsRoot.Database.GetItem(this.CreateBlankForm.Checked ? string.Empty : this.multiTree.Selected);
        this.RenderFormPreview(form, output);
        builder.Append(output.InnerWriter.ToString());
        builder.Append("</p>");
        return builder.ToString();
      }
      return string.Empty;
    }

    protected string GetUniqueName(string name)
    {
      string str = name;
      int num = 0;
      while (this.FormsRoot.Database.GetItem(this.FormsRoot.Paths.ContentPath + "/" + str) != null)
      {
        str = name + ++num;
      }
      return str;
    }

    private string GetWindowKey(string url)
    {
      if ((url == null) || (url.Length == 0))
      {
        return string.Empty;
      }
      string text = url;
      int index = text.IndexOf("?xmlcontrol=");
      if (index >= 0)
      {
        index = text.IndexOf("&", index);
        if (index >= 0)
        {
          text = StringUtil.Left(text, index);
        }
      }
      else if (text.IndexOf("?") >= 0)
      {
        text = StringUtil.Left(text, text.IndexOf("?"));
      }
      if (text.StartsWith(Sitecore.Web.WebUtil.GetServerUrl(), StringComparison.OrdinalIgnoreCase))
      {
        text = StringUtil.Mid(text, Sitecore.Web.WebUtil.GetServerUrl().Length);
      }
      return text;
    }

    protected virtual void Localize()
    {
      this.CreateForm["Header"] = DependenciesManager.ResourceManager.Localize("CREATE_NEW_FORM");
      this.CreateForm["Text"] = DependenciesManager.ResourceManager.Localize("CREATE_BLANK_OR_COPY_EXISTING_FORM");
      this.FormNameLiteral.Text = DependenciesManager.ResourceManager.Localize("FORM_TEXT") + ":";
      this.CreateBlankForm.Header = DependenciesManager.ResourceManager.Localize("CREATE_BLANK_FORM");
      this.ChooseForm.Header = DependenciesManager.ResourceManager.Localize("SELECT_FORM_TO_COPY");
      this.SelectForm["Header"] = DependenciesManager.ResourceManager.Localize("SELECT_FORM");
      this.SelectForm["Text"] = DependenciesManager.ResourceManager.Localize("COPY_EXISTING_FORM");
      this.AnalyticsPage["Header"] = DependenciesManager.ResourceManager.Localize("ANALYTICS");
      this.AnalyticsPage["Text"] = DependenciesManager.ResourceManager.Localize("CHOOSE_WHICH_ANALYTICS_OPTIONS_WILL_BE_USED");
      this.AnalyticsOptions.Header = DependenciesManager.ResourceManager.Localize("GOAL");
      this.CreateGoal.Header = DependenciesManager.ResourceManager.Localize("CREATE_NEW_GOAL");
      this.GoalName.Value = DependenciesManager.ResourceManager.Localize("FORM_NAME_FORM_COMPLETED");
      this.GoalNameLiteral.Text = DependenciesManager.ResourceManager.Localize("NAME") + ":";
      this.PointsLiteral.Text = DependenciesManager.ResourceManager.Localize("ENGAGEMENT_VALUE") + ":";
      this.SelectGoal.Header = DependenciesManager.ResourceManager.Localize("SELECT_EXISTING_GOAL");
      this.SelectGoalLiteral.Text = DependenciesManager.ResourceManager.Localize("SELECT_NEW_OR_EXISTEN_GOAL");
      this.DropoutOptions.Header = DependenciesManager.ResourceManager.Localize("DROPOUT_TRACKING");
      this.EnableFormDropoutTracking.Header = DependenciesManager.ResourceManager.Localize("ENABLE_FORM_DROPOUT_TRACKING");
      this.EnableFormDropoutTrackingLiteral.Text = DependenciesManager.ResourceManager.Localize("SELECT_IT_TO_TRACK_INFORMATION_ENTERED_IN_FORM");
      this.EnableDropoutSavedToLiteral.Text = DependenciesManager.ResourceManager.Localize("IF_ENABLED_ANY_DATA_ENTERED_IS_SAVED_IN_ANALYTICS");
      this.ConfirmationPage["Header"] = DependenciesManager.ResourceManager.Localize("CONFIRMATION");
      this.ConfirmationPage["Text"] = DependenciesManager.ResourceManager.Localize("CONFIRM_CONFIGURATION_OF_NEW_FORM");
      this.ChoicesLiteral.Text = DependenciesManager.ResourceManager.Localize("YOU_HAVE_SELECTED_THE_FOLLOWING_SETTINGS");
    }

    [HandleMessage("form:creategoal", true)]
    public void OnCreateGoalChanged(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      this.Goals.Disabled = true;
      this.Goals.Disabled = this.CreateGoal.Checked;
      this.GoalName.ReadOnly = false;
      this.Points.ReadOnly = false;
      this.GoalName.ReadOnly = !this.CreateGoal.Checked;
      this.Points.ReadOnly = !this.CreateGoal.Checked;
      if (this.Goals.Disabled)
      {
        this.Goals.Value = string.Empty;
      }
      this.GoalName.Style["color"] = this.CreateGoal.Checked ? "black" : "#999999";
      this.Points.Style["color"] = this.CreateGoal.Checked ? "black" : "#999999";
      SheerResponse.SetOuterHtml(this.GoalName.ID, this.GoalName);
      SheerResponse.SetOuterHtml(this.Points.ID, this.Points);
      SheerResponse.Eval("$j('.Range').numeric();$$('.scComboboxDropDown')[0].disabled = " + this.Goals.Disabled.ToString().ToLower() + ";");
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      if (!Context.ClientPage.IsEvent)
      {
        Context.ClientPage.ClientScript.RegisterClientScriptInclude("jquery", "/sitecore modules/web/web forms for marketers/scripts/jquery.js");
        Context.ClientPage.ClientScript.RegisterClientScriptInclude("jquery-ui.min", "/sitecore modules/web/web forms for marketers/scripts/jquery-ui.min.js");
        Context.ClientPage.ClientScript.RegisterClientScriptInclude("jquery-ui-i18n", "/sitecore modules/web/web forms for marketers/scripts/jquery-ui-i18n.js");
        Context.ClientPage.ClientScript.RegisterClientScriptInclude("json2.min", "/sitecore modules/web/web forms for marketers/scripts/json2.min.js");
        Context.ClientPage.ClientScript.RegisterClientScriptInclude("head.load.min", "/sitecore modules/web/web forms for marketers/scripts/head.load.min.js");
        Context.ClientPage.ClientScript.RegisterClientScriptInclude("sc.webform", "/sitecore modules/web/web forms for marketers/scripts/sc.webform.js?v=17072012");
        this.Localize();
        this.ChooseForm.Checked = true;
        this.CreateGoal.Checked = true;
        this.EnableFormDropoutTracking.Checked = true;
        this.AnalyticsOptions.Visible = true;
        this.DropoutOptions.Visible = true;
        this.Goals.Value = string.Empty;
        ThemesManager.RegisterCssScript(null, this.FormsRoot, this.FormsRoot);
        this.EbFormName.Value = this.GetUniqueName("Example Form");
        MultiTreeView view = new MultiTreeView
        {
          Roots = Sitecore.Form.Core.Utility.Utils.GetFormRoots(),
          Filter = "Contains('{C0A68A37-3C0A-4EEB-8F84-76A7DF7C840E},{A87A00B1-E6DB-45AB-8B54-636FEC3B5523},{FFB1DA32-2764-47DB-83B0-95B843546A7E}', @@templateid)",
          ID = this.multiTreeID,
          DataViewName = "Master",
          TemplateID = Sitecore.Form.Core.Configuration.IDs.FormTemplateID.ToString(),
          IsFullPath = true
        };
        this.multiTree = view;
        this.ExistingForms.Controls.Add(this.multiTree);
      }
      else
      {
        this.multiTree = this.ExistingForms.FindControl(this.multiTreeID) as MultiTreeView;
      }
    }

    protected override void OnNext(object sender, EventArgs formEventArgs)
    {
      if (base.NextButton.Header == "Create")
      {
        this.SaveForm();
        SheerResponse.SetModified(false);
      }
      base.Next();
    }

    private void RemoveScipts(HtmlNode node)
    {
      for (int i = 0; i < node.ChildNodes.Count; i++)
      {
        HtmlNode node2 = node.ChildNodes[i];
        this.RemoveScipts(node2);
        if (node2.Name.ToLower() == "script")
        {
          node2.InnerHtml = " ";
        }
      }
    }

    protected string RenderBeginSection(string name)
    {
      StringBuilder builder = new StringBuilder();
      builder.Append("<fieldset class='scfGroupSection' >");
      builder.Append("<legend>");
      builder.Append(DependenciesManager.ResourceManager.Localize(name));
      builder.Append("</legend>");
      return builder.ToString();
    }

    protected string RenderEndSection() =>
        "</fieldset>";

    protected void RenderFormPreview(Item form, HtmlTextWriter output)
    {
      HtmlTextWriter writer = new HtmlTextWriter(new StringWriter());
      FormRender render = new FormRender
      {
        FormID = (form != null) ? form.ID.ToString() : string.Empty,
        IsFastPreview = true
      };
      render.InitControls();
      render.RenderControl(writer);
      if (writer.InnerWriter.ToString() != string.Empty)
      {
        string html = writer.InnerWriter.ToString();
        HtmlDocument document = new HtmlDocument();
        using (new ThreadCultureSwitcher(Language.Parse("en").CultureInfo))
        {
          document.LoadHtml(html);
        }
        this.RemoveScipts(document.DocumentNode);
        html = Regex.Replace(document.DocumentNode.InnerHtml, "on\\w*=\".*?\"", string.Empty);
        output.Write(html);
        output.Write("<img height='1px' alt='' src='/sitecore/images/blank.gif' width='1' border='0'onload='javascript:Sitecore.Wfm.Utils.zoom(this.previousSibling)'/>");
      }
    }

    protected virtual string RenderSetting()
    {
      StringBuilder builder = new StringBuilder();
      if (this.RenderConfirmationFormSection)
      {
        builder.Append(this.RenderBeginSection("FORM"));
        builder.Append(this.GenerateItemSetting());
        builder.Append(this.RenderEndSection());
      }
      string str = (string.Compare(Sitecore.Web.WebUtil.GetQueryString("mode"), StaticSettings.DesignMode, true) != 0) ? this.GeneratePreview() : string.Empty;
      if (this.AnalyticsSettings.IsAnalyticsAvailable)
      {
        builder.Append(this.RenderBeginSection("ANALYTICS"));
        builder.Append(this.GenerateAnalytics());
        builder.Append(this.RenderEndSection());
        string str2 = this.GenerateFutherInfo();
        if (str2.Length > 0)
        {
          builder.Append(this.RenderBeginSection("FURTHER_INFORMATION"));
          builder.Append(str2);
          builder.Append(this.RenderEndSection());
        }
      }
      if (!string.IsNullOrEmpty(str))
      {
        builder.Append(this.RenderBeginSection("PREVIEW"));
        builder.Append(str);
        builder.Append(this.RenderEndSection());
      }
      return builder.ToString();
    }

    protected virtual void SaveAnalytics(FormItem form, string goalID)
    {
      if (this.AnalyticsSettings.IsAnalyticsAvailable)
      {
        ITracking tracking = form.Tracking;
        tracking.Update(true, this.EnableFormDropoutTracking.Checked);
        Item item = this.CreateGoal.Checked ? this.GoalsRoot.Add(this.GoalName.Value, new TemplateID(Sitecore.Form.Core.Configuration.IDs.GoalTemplateID)) : this.GoalsRoot.Database.GetItem(goalID);
        item.Editing.BeginEdit();
        if (this.CreateGoal.Checked)
        {
          item["Points"] = string.IsNullOrEmpty(this.Points.Value) ? "0" : this.Points.Value;
        }
        item["__Workflow state"] = "{EDCBB550-BED3-490F-82B8-7B2F14CCD26E}";
        item.Editing.EndEdit();
        tracking.AddEvent(item.ID.Guid);
        form.BeginEdit();
        form.InnerItem.Fields["__Tracking"].Value = tracking.ToString();
        form.EndEdit();
      }
      else if (form.InnerItem.Fields["__Tracking"] != null)
      {
        form.BeginEdit();
        form.InnerItem.Fields["__Tracking"].Value = "<tracking ignore=\"1\"/>";
        form.EndEdit();
      }
    }

    protected virtual void SaveForm()
    {
      string goalID = this.Goals.Value;
      Item formsRoot = this.FormsRoot;
      Assert.IsNotNull(formsRoot, "forms root");
      string queryString = Sitecore.Web.WebUtil.GetQueryString("la");
      Language contentLanguage = Context.ContentLanguage;
      if (!string.IsNullOrEmpty(queryString))
      {
        Language.TryParse(Sitecore.Web.WebUtil.GetQueryString("la"), out contentLanguage);
      }
      Item item = this.FormsRoot.Database.GetItem(this.CreateBlankForm.Checked ? string.Empty : this.multiTree.Selected, contentLanguage);
      string name = this.EbFormName.Value;
      string copyName = Sitecore.Data.Items.ItemUtil.ProposeValidItemName(name);
      if (item != null)
      {
        Item oldForm = item;
        item = Context.Workflow.CopyItem(item, formsRoot, copyName, new ID(), true);
        FormItemSynchronizer.UpdateIDReferences(oldForm, item);
      }
      else
      {
        if (formsRoot.Language != contentLanguage)
        {
          formsRoot = this.FormsRoot.Database.GetItem(formsRoot.ID, contentLanguage);
        }
        item = Context.Workflow.AddItem(copyName, new TemplateID(Sitecore.Form.Core.Configuration.IDs.FormTemplateID), formsRoot);
        item.Editing.BeginEdit();
        item.Fields[Sitecore.Form.Core.Configuration.FieldIDs.ShowFormTitleID].Value = "1";
        item.Editing.EndEdit();
      }
      item.Editing.BeginEdit();
      item[Sitecore.Form.Core.Configuration.FieldIDs.FormTitleID] = name;
      item[Sitecore.Form.Core.Configuration.FieldIDs.DisplayNameFieldID] = name;
      item.Editing.EndEdit();
      this.SaveAnalytics(item, goalID);
      base.ServerProperties[this.newFormUri] = item.Uri.ToString();
      Registry.SetString("/Current_User/Dialogs//sitecore/shell/default.aspx?xmlcontrol=Forms.FormDesigner", "1250,500");
      SheerResponse.SetDialogValue(item.Uri.ToString());
    }

    private string DatabaseName =>
        Sitecore.Web.WebUtil.GetQueryString("db", "master");

    protected virtual Item FormsRoot
    {
      get
      {
        if (this.formsRoot == null)
        {
          this.formsRoot = Factory.GetDatabase(this.DatabaseName).GetItem(this.Root);
        }
        return this.formsRoot;
      }
    }

    protected virtual Item GoalsRoot
    {
      get
      {
        if (this.goalsRoot == null)
        {
          this.goalsRoot = Factory.GetDatabase(this.DatabaseName).GetItem(StaticSettings.GoalsRootID);
        }
        return this.goalsRoot;
      }
    }

    protected virtual bool RenderConfirmationFormSection =>
        true;

    private string Root =>
        Sitecore.Web.WebUtil.GetQueryString("root", StaticSettings.GlobalFormsRootID);
  }
}
