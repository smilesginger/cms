﻿using System;
using System.Collections.Specialized;
using SiteServer.BackgroundPages.Core;
using SiteServer.CMS.Caches;
using SiteServer.CMS.Core;
using SiteServer.CMS.Core.RestRoutes.Preview;
using SiteServer.CMS.Database.Attributes;
using SiteServer.CMS.Database.Core;
using SiteServer.CMS.Database.Models;
using SiteServer.CMS.Plugin;
using SiteServer.Plugin;
using SiteServer.Utils;

namespace SiteServer.BackgroundPages.Cms
{
    public class PageContentAddHandler : BaseHandler
    {
        public static string GetRedirectUrl(int siteId, int channelId, int contentId)
        {
            return PageUtils.GetCmsWebHandlerUrl(siteId, nameof(PageContentAddHandler), new NameValueCollection
            {
                {"channelId", channelId.ToString()},
                {"contentId", contentId.ToString()}
            });
        }

        protected override object Process()
        {
            var siteId = AuthRequest.SiteId;
            var channelId = AuthRequest.ChannelId;
            var contentId = AuthRequest.ContentId;

            var siteInfo = SiteManager.GetSiteInfo(siteId);
            var channelInfo = ChannelManager.GetChannelInfo(siteId, channelId);
            var tableName = ChannelManager.GetTableName(siteInfo, channelInfo);
            var styleInfoList = TableStyleManager.GetContentStyleInfoList(siteInfo, channelInfo);

            var form = AuthRequest.HttpRequest.Form;

            var dict = BackgroundInputTypeParser.SaveAttributes(siteInfo, styleInfoList, form, ContentAttribute.AllAttributes.Value);
            var contentInfo = new ContentInfo(dict)
            {
                ChannelId = channelId,
                SiteId = siteId,
                AddUserName = AuthRequest.AdminName,
                LastEditUserName = AuthRequest.AdminName,
                LastEditDate = DateTime.Now
            };

            //contentInfo.GroupNameCollection = ControlUtils.SelectedItemsValueToStringCollection(CblContentGroups.Items);
            var tagCollection = TagUtils.ParseTagsString(form["TbTags"]);

            contentInfo.Title = form["TbTitle"];
            var formatString = TranslateUtils.ToBool(form[ContentAttribute.Title + "_formatStrong"]);
            var formatEm = TranslateUtils.ToBool(form[ContentAttribute.Title + "_formatEM"]);
            var formatU = TranslateUtils.ToBool(form[ContentAttribute.Title + "_formatU"]);
            var formatColor = form[ContentAttribute.Title + "_formatColor"];
            var theFormatString = ContentUtility.GetTitleFormatString(formatString, formatEm, formatU, formatColor);
            contentInfo.Set(ContentAttribute.GetFormatStringAttributeName(ContentAttribute.Title), theFormatString);
            //foreach (ListItem listItem in CblContentAttributes.Items)
            //{
            //    var value = listItem.Selected.ToString();
            //    var attributeName = listItem.Value;
            //    contentInfo.Set(attributeName, value);
            //}
            //contentInfo.LinkUrl = TbLinkUrl.Text;
            contentInfo.AddDate = TranslateUtils.ToDateTime(form["TbAddDate"]);
            contentInfo.Checked = false;
            contentInfo.Tags = TranslateUtils.ObjectCollectionToString(tagCollection, " ");

            foreach (var service in PluginManager.Services)
            {
                try
                {
                    service.OnContentFormSubmit(new ContentFormSubmitEventArgs(siteId, channelId, contentInfo.Id, TranslateUtils.ToDictionary(form), contentInfo));
                }
                catch (Exception ex)
                {
                    LogUtils.AddErrorLog(service.PluginId, ex, nameof(IService.ContentFormSubmit));
                }
            }

            contentInfo.Id = DataProvider.ContentRepository.InsertPreview(tableName, siteInfo, channelInfo, contentInfo);

            return new
            {
                previewUrl = ApiRoutePreview.GetContentPreviewUrl(siteId, channelId, contentId, contentInfo.Id)
            };
        }
    }
}
