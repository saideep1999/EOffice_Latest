function RepTableDataArray(tableName, data) {
    //Check if table dose not exist then return error message.
    try {
        if (tableName == null) {
            alert("table not exist.");
        }

        //Check Data Table has if already initialize then need to destroy first!
        if ($.fn.DataTable.isDataTable(tableName)) {
            tableName.DataTable().destroy();
            tableName.empty();
        }

        var Columns = [];
        var TableHeader = "<thead><tr>";
        $.each(data[0], function (key, value) {
            Columns.push({ "data": key });
            TableHeader += "<th>" + key + "</th>";
        });
        TableHeader += "</thead></tr>";
        tableName.append(TableHeader);

        tableName.dataTable({
            "destroy": true,
            "bLengthChange": false,
            "aaData": data,
            "bInfo": false,
            "bPaginate": false,
            "bFilter": true,
            "paging": false,
            "order": [],
            "columns": Columns,
            "dom": "Bfrtip",
            "buttons": [{
                "extend": 'excel',
                "title": function () {
                    var title = "Student Record.";
                    return title;
                },
                "filename": 'StudentRecords'
            }]
        });
    }
    catch (ex) {
        alert(ex.message);
    }
};