using System.Text;

namespace COMMON;

public class Pagination
{
    public string ContainerId { get; set; } = "pagination";
    public string ContainerClass { get; set; } = "pagination";
    public string ListContainerClass { get; set; } = "page-list";
    public string ListClass { get; set; } = "page-item";
    public string LinkClass { get; set; } = "page-link";
    public string UrlTemplate { get; set; }
    public string PageParam { get; set; } = "page";

    public int CurrentPage { get; set; }
    public int TotalPage { get; set; } = 1;
    public int ShowCount { get; set; } = 2;
    public int PagingCount { get; set; } = 3;

    private string PrevHref => Url(CurrentPage == 1 ? CurrentPage : CurrentPage - 1);

    private string NextHref => Url(CurrentPage == TotalPage ? CurrentPage : CurrentPage + 1);

    private int MinPage =>
        CurrentPage < (2 * ShowCount)
            ? 1
            : (CurrentPage > TotalPage - (ShowCount + 1) ? TotalPage - (2 * ShowCount) : CurrentPage - ShowCount);


    private string Url(string value)
    {
        return UrlHelper.AddParam(UrlTemplate, PageParam, value);
    }

    private string Url(int value)
    {
        return Url(value.ToString());
    }

    public string Result
    {
        get
        {
            var sb = new StringBuilder();

            if (TotalPage > 1)
            {
                sb.Append(
                    $"<nav {(string.IsNullOrWhiteSpace(ContainerId) ? "" : $"id=\"{ContainerId}\"")} class=\"{ContainerClass}\"><ul class=\"{ListContainerClass}\">");

                sb.Append(@$"<li class=""{ListClass}"">
                            <a href=""{PrevHref}"" class=""{LinkClass}"" {(CurrentPage == 1 ? "disabled" : "")}>
                                <i class=""fa-solid fa-chevron-right fa-rotate-180""></i>
                            </a>
                        </li>");

                if (MinPage > 1)
                {
                    sb.Append(@$"<li class=""{ListClass}""><a href=""{Url(1)}"" class=""{LinkClass}"">1</a></li>");

                    sb.Append(
                        @$"<li class=""{ListClass}""><a href=""{Url(CurrentPage > PagingCount ? CurrentPage - PagingCount : 1)}""class=""{LinkClass}"">...</a></li>");
                }

                for (var p = MinPage; p < MinPage + 2 * ShowCount + 1; p++)
                {
                    var classList = p == CurrentPage ? $"{ListClass} active" : ListClass;
                    sb.Append(@$"<li class=""{classList}""><a href=""{Url(p)}""class=""{LinkClass}"">{p}</a></li>");
                }


                if (MinPage < TotalPage - (2 * ShowCount))
                {
                    sb.Append(
                        @$"<li class=""{ListClass}""><a href=""{Url(CurrentPage < TotalPage - (PagingCount - 1) ? CurrentPage + PagingCount : TotalPage)}"" class=""{LinkClass}"">...</a></li>");

                    sb.Append(
                        @$"<li class=""{ListClass}""><a href=""/{Url(TotalPage)}"" class=""{LinkClass}"">{TotalPage}</a></li>");
                }

                sb.Append(@$"<li class=""{ListClass}"">
                            <a href=""{NextHref}"" class=""{LinkClass}"" {(CurrentPage == TotalPage ? "disabled" : "")}>
                                <i class=""fa-solid fa-chevron-right""></i>
                            </a>
                        </li>");

                sb.Append("</ul></nav>");
            }

            var html = sb.ToString();
            return html;
        }
    }
}