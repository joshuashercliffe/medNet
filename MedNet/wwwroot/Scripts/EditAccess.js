$("#selectUserType").change(function () {
    var MakeTypeahead = function (data) {
        $('.typeahead').typeahead({
            items: 4,
            fitToElement: true,
            source: data,
            matcher: function (item) {
                var it = this.displayText(item).toLowerCase();
                var query = this.query.toLowerCase();
                it = it.replace(/-/g, '');
                query = query.replace(/-/g, '');
                query = query.replace(/_/g, '');
                return ~it.indexOf(query)
            }
        });
    }

    $('.typeahead').typeahead('destroy');
    $.get("GetAllTypeIDs?type=" + $(this).val(), function (data) {
        MakeTypeahead(data);
    }, 'json');

    if ($(this).val() == "") {
        $('#userID').hide();
    }
    else if ($(this).val() == "Doctor") {
        $('#userID').show();
        $("label[for='UserID']").text("Enter the MINC of the Doctor");
        $("#userIDInput").attr("placeholder", "CA##-####-####");
        $("#userIDInput").inputmask("AA99-9999-9999");
    }
    else if ($(this).val() == "Pharmacist") {
        $('#userID').show();
        $("label[for='UserID']").text("Enter the Registration number of the Pharmacist");
        $("#userIDInput").attr("placeholder", "####-####-####");
        $("#userIDInput").inputmask("9999-9999-9999");
    }
    else {
        $('#userID').show();
        $("label[for='UserID']").text("Enter the CSMLS ID of the MLT");
        $("#userIDInput").attr("placeholder", "####-###");
        $("#userIDInput").inputmask("9999-999");
    }
});

var RenderAccessList = function () {
    var params = new window.URLSearchParams(window.location.search);
    if (params.has('transID')) {
        var transID = params.get('transID');
        $.get("GetAccessListTransID?transID=" + transID, function (data) {
            var tableHtml = ' <h4 class="pt-1">Access List</h4> \
                <table class="table table-sm"> \
                        <thead> \
                        <tr> \
                            <th scope="col">Type</th> \
                            <th scope="col">Name</th> \
                            <th scope="col">ID</th> \
                        </tr> \
                        </thead > \
                        <tbody>';
            for (var i in data) {
                var user = data[i];
                console.log(user);
                tableHtml += '<tr>';
                tableHtml += '<td>' + user.userType + '</td>';
                tableHtml += '<td>' + user.userName + '</td>';
                tableHtml += '<td>' + user.userID + '</td>';
                tableHtml += '</tr>';
            }
            tableHtml += '</tbody> \
                    </table >';
            $("#accessList").html(tableHtml);
        })
    }
}

$(function () {
    //render Access List table
    RenderAccessList();

    //trigger selector change
    $("#selectUserType").trigger("change");

    //grantAccessBtn click event
    $("#grantAccessBtn").click(function (e) {
        var dataToPost = {
            UserType: $("#selectUserType").val(),
            UserID: $("#userIDInput").val()
        };
        var params = new window.URLSearchParams(window.location.search);
        if (params.has('transID')) {
            dataToPost.TransID = params.get('transID')
        }

        $.post("GrantAccessToUser", dataToPost)
            .done(function (response, status, jqxhr) {
                // this is the "success" callback
                $("#message").html(
                    '<div class="alert alert-warning alert-dismissible fade show mb-0 mt-2" role="alert">' +
                    '<button type="button" class="close" data-dismiss="alert" aria-label="Close">' +
                    '<span aria-hidden="true">&times;</span>' +
                    '</button>' +
                    response.message +
                    '</div>')
                if (response.hasOwnProperty('newtransid')) {
                    window.setTimeout(function () {
                        window.location.replace('EditAccess?transID=' + response.newtransid)
                    }, 1000)
                }
            })
            .fail(function (jqxhr, status, error) {
                // this is the ""error"" callback
                $("#message").html(
                    '<div class="alert alert-warning alert-dismissible fade show mb-0 mt-2" role="alert">' +
                    '<button type="button" class="close" data-dismiss="alert" aria-label="Close">' +
                    '<span aria-hidden="true">&times;</span>' +
                    '</button>' +
                    'There was an internal error while processing the request.' +
                    '</div>')
            });
    });

    //revolkeAccessBtn click event
    $("#revokeAccessBtn").click(function (e) {
        var dataToPost = {
            UserType: $("#selectUserType").val(),
            UserID: $("#userIDInput").val()
        };
        var params = new window.URLSearchParams(window.location.search);
        if (params.has('transID')) {
            dataToPost.TransID = params.get('transID')
        }

        $.post("RevokeAccessFromUser", dataToPost)
            .done(function (response, status, jqxhr) {
                // this is the "success" callback
                $("#message").html(
                    '<div class="alert alert-warning alert-dismissible fade show mb-0 mt-2" role="alert">' +
                    '<button type="button" class="close" data-dismiss="alert" aria-label="Close">' +
                    '<span aria-hidden="true">&times;</span>' +
                    '</button>' +
                    response.message +
                    '</div>')
                if (response.hasOwnProperty('newtransid')) {
                    window.setTimeout(function () {
                        window.location.replace('EditAccess?transID=' + response.newtransid)
                    }, 1000)                }
            })
            .fail(function (jqxhr, status, error) {
                // this is the ""error"" callback
                $("#message").html(
                    '<div class="alert alert-warning alert-dismissible fade show mb-0 mt-2" role="alert">' +
                    '<button type="button" class="close" data-dismiss="alert" aria-label="Close">' +
                    '<span aria-hidden="true">&times;</span>' +
                    '</button>' +
                    'There was an internal error while processing the request.' +
                    '</div>')
            });
    });
});