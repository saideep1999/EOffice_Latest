///// <reference path="GetTableJson.js" />
function RepTableDataJson(tableName, data,title,fileName) {
    //debugger;
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

        var parseJSONResult = jQuery.parseJSON(data);

        if (parseJSONResult != null && parseJSONResult.length > 0) {
            //Get dynmmic column.
            var dynamicColumns = [];
            var i = 0;
            $.each(parseJSONResult[0], function (key, value) {
                var obj = { sTitle: key };
                dynamicColumns[i] = obj;
                i++;
            });
            //fetch all records from JSON result and make row data set.
            var rowDataSet = [];
            var i = 0;
            $.each(parseJSONResult, function (key, value) {
                var rowData = [];
                var j = 0;
                $.each(parseJSONResult[i], function (key, value) {
                    rowData[j] = value;
                    j++;
                });
                rowDataSet[i] = rowData;

                i++;
            });
            tableName.dataTable({
                "destroy": true,
                "columns": dynamicColumns,
                "bLengthChange": false,
                "bInfo": false,
                "bPaginate": false,
                "bAutoWidth": true,
                scrollY: '55vh',
                scrollCollapse: true,
                //"bFilter": false,
                "paging": false,
                "order": [],
                "aaData": response,
                //"destroy": true,
                //"scrollY": '55vh',
                //"scrollCollapse": true,
                //"bLengthChange": false,
                //"aaData": rowDataSet,
                //"bInfo": true,
                //"bPaginate": false,
                //"bFilter": true,
                //"paging": false,
                //"columns": dynamicColumns,
                //"deferRender": true,

            });
        }

        
    }
    catch (ex) {
        alert(ex.message);
    }
}