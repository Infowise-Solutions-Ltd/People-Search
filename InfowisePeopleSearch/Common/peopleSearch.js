function IWPSReorderFields(ddl, hdnID) {
    var tbl = ddl.parentNode.parentNode.parentNode;
    var row = IWPSGetRowWithNumber(tbl.childNodes, ddl.value, ddl.parentNode.parentNode);
    if (row == null)
        return;
    row.childNodes[1].firstChild.value = ddl.getAttribute ? ddl.getAttribute("oldVal") : ddl.oldVal;
    if (ddl.setAttribute) {
        row.childNodes[1].firstChild.setAttribute("oldVal", ddl.getAttribute("oldVal"));
        ddl.setAttribute("oldVal", ddl.value);
    }
    else {
        row.childNodes[1].firstChild.oldVal = ddl.oldVal;
        ddl.oldVal = ddl.value;
    }

    IWPSSelectFields(ddl, hdnID);
}

function IWPSSelectFieldsById(chkID, hdnID) {
    IWPSSelectFields(document.getElementById(chkID), hdnID);
}
function IWPSSelectFields(chk, hdnID) {
    var hdn = document.getElementById(hdnID);
    if (hdn == null)
        return;
    var newVal = "";
    var tbl = getParentOfType(chk, "table");
    for (var i = 0; i < tbl.getElementsByTagName("tr").length; i++) {
        var order = i + 1;
        var row = IWPSGetRowWithNumber(tbl.getElementsByTagName("tr"), order);
        if (row != null && IWPSGetChildNodeByIndex(IWPSGetChildNodeByIndex(IWPSGetChildNodeByIndex(row, "td", 0), "span", 0), "input", 0).checked) {
            var internalName = row.getAttribute?IWPSGetChildNodeByIndex(IWPSGetChildNodeByIndex(row, "td", 0), "span", 0).getAttribute("internalName"):IWPSGetChildNodeByIndex(IWPSGetChildNodeByIndex(row, "td", 0), "span", 0).internalname;
            newVal += internalName + ";" + IWPSGetChildNodeByIndex(IWPSGetChildNodeByIndex(row, "td", 2), "input", 0).value + ";" + order + "|";
        }
    }

    hdn.value = newVal;
}

function IWPSGetChildNodeByIndex(tbody, tagName, index) {
    var i = 0;
    for (var y = 0; y < tbody.childNodes.length; y++) {
        if (tbody.childNodes[y].nodeType == 1 && tbody.childNodes[y].tagName.toLowerCase() == tagName) {
            if (i == index)
                return tbody.childNodes[y];
            i++;
        }
    }
}

function IWPSGetRowWithNumber(rows, num, oldRow) {
    for (var i = 0; i < rows.length; i++) {
        if (rows[i].getElementsByTagName("td")[1].firstChild.value == num
             && (oldRow == null || rows[i] != oldRow))
            return rows[i];
    }
    return null;
}

function getParentOfType(childNode, parentName) {
    if (childNode.parentNode.tagName.toLowerCase() == parentName)
        return childNode.parentNode;
    return getParentOfType(childNode.parentNode, parentName);
}