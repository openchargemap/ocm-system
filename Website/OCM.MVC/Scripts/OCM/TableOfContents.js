function prepareTableOfContents() {
    // from http://css-tricks.com/automatic-table-of-contents/
    var ToC = "<nav role='navigation'>" +
        "<h2>Contents:</h2>" +
        "<ul class='nav nav-pills nav-stacked'>";

    var newLine, el, title, link;

    $("article h2").each(function () {

        el = $(this);
        title = el.text();
        link = "#" + el.attr("id");

        newLine =
          "<li>" +
            "<a href='" + link + "'>" +
              title +
            "</a>" +
          "</li>";

        ToC += newLine;

    });

    ToC +=
       "</ul>" +
      "</nav>";

    $("#toc").html(ToC);
}