// The API key
var api_key = "";
// OpenAI URL:
var openai_url = "https://api.openai.com/v1/chat/completions";
// GPT Temprature
var temprature = 0.6;
// GPT Model Name
var model_name = "gpt-4";//"gpt-3.5-turbo";
var gpt3modelname = "gpt-3.5-turbo";
// Number of responses to generate:
var num_responses = 1;
// Max tokens to generate:
var max_tokens = 4096;

// Figure paths
var template = {
    "Internal Examination": ["Scalp, skull and dura", "Brain", "Spinal cord", "Heart", "Pericardium", "Epicardium", "Myocardium", "Endocardium", "Cardiac valves", "Coronary arteries", "Aorta and major branches", "Venae cavae", "Peripheral veins", "Mediastinum", "Pleura", "Mouth and Pharynx"],
    "External Examination": ["Head & Neck", "Thorax & Abdomen", "Anus & Back", "External Genitalia", "Left Upper Limb", "Right Upper Limb", "Left Lower Limb", "Right Lower Limb"]
};
var gptSystemInstruction = `You are a medical assistant, analyse the provided report in the user prompt. Identify the causes of injuries, trauma, and diseases and summarise them in a JSON file.

Consider these rules in your response:
- Make a json file, without any explanation. There is no hierarchy in the file, just key and values.
- In json file, keys are {"Head & Neck", "Thorax & Abdomen", "Anus & Back", "External Genitalia", "Left Upper Limb","Right Upper Limb","Left Lower Limb","Right Lower Limb","Scalp, skull and dura", "Brain", "Spinal cord", "Heart", "Pericardium", "Epicardium", "Myocardium", "Endocardium", "Cardiac valves", "Coronary arteries", "Aorta and major branches", "Venae cavae", "Peripheral veins", "Mediastinum", "Pleura","Mouth and Pharynx"}. Keys are case-sensetive.
- Find and analyse the relevant information, to conclude for each key (organ), write a summary of the findings for each key as its value.
- Along with the summary of findings, reference the line number (just the number) that you extract that finding in brackets [].
- If you cannot find anything about that organ, just assign "Unremarkable" for its value.
`;

// Undo/Redo Functionality Global Variables
var sumtextUID = 1;
var undoMap = new Map();
var redoMap = new Map();
// UI Global Variables
var lineSrc;
var lineDst;
var drawingCanvas;
var ctx;
// basic functionalities
var lastRightClickReceiver;

// Pre-computed texts

//comments sections
var commentsMap = new Map();
var commentUID = 1;
//

var tempGPTAnswer = {

};

//
function extractNumbersFromString(string) {
    const regex = /\[.*?(\d+)\]/g;
    const matches = string.match(regex);

    if (!matches) {
        return [];
    }
    let numbers = [];
    numbers = matches.map(match => parseInt(match.substring(1, match.length - 1)));

    return numbers;
}
function getValuesByKey(arr, key) {
    return arr.reduce((values, tuple) => {
        if (tuple[0] === key) {
            values.push(tuple[1]);
        }
        return values;
    }, []);
}

function tagAnswer(answer) {
    if (answer == undefined || answer == null || answer == "") return "<span id=\"sumtext" + (sumtextUID++) + "\" class=\"summarisedtext\" >&nbsp;&nbsp;</span>";

    let taggedAnswer = "";
    //find [number] in answer
    let regex = /\[\s*\d+\s*\]/;
    let numbers = extractNumbersFromString(answer);
    let answerArray = answer.split(regex);
    //if(answerArray.length > 1) console.log(answerArray);
    for (let i = 0; i < answerArray.length; i++) {

        if (answerArray[i].trim().length === 0) continue;
        if (inputTexts[numbers[0] - 1] != undefined) taggedAnswer += "<span id=\"sumtext" + (sumtextUID++) + "\"class=\"summarisedtext\" data-ref=\"" + numbers + "\" >" + answerArray[i];//title=\"" + inputTexts[numbers[i] - 1] + "\">" + answerArray[i];
        else taggedAnswer += "<span id=\"sumtext" + (sumtextUID++) + "\"class=\"summarisedtext\">" + answerArray[i];
        let figsIncluded = [];
        for (const n of numbers) {
            let values = getValuesByKey(textToFigs, n - 1);
            if (values != undefined) {
                for (const figNum of values) {
                    if (!figsIncluded.includes(figNum)) {
                        figsIncluded.push(figNum);
                        taggedAnswer += "[Fig" + (figNum + 1) + "]";
                    }
                }
            }
        }

        taggedAnswer += "</span>";
    }
    return taggedAnswer;
}

//Generate Summaries
function GenerateSummaries(gptAnswer) {
    // convert template array to html
    var templateHTML = "";
    for (var key in template) {
        templateHTML += "<h3>" + key + ":</h3>";
        for (var i = 0; i < template[key].length; i++) {
            // check if gptAnswer[template[key][i]] is defined and not null
            let answer = gptAnswer[template[key][i]];
            if (answer == undefined && answer == null) {
                answer = "";
            }
            templateHTML += "<p>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + template[key][i] + ": " + tagAnswer(answer) + "</p>";
        }
    }
    $("#template").html(templateHTML);
    convertRefToTags();
    addTooltipToRefs();
    addListeneresToFigLabels();
}

// Analyse Input
function analyzeAndWrite() {
    console.log(inputTexts);
    let userPrompt = NumericBulletList(inputTexts);
    if (userPrompt.trim().length == 0) userPrompt = "No information provided.";
    console.log("User prompt:" + userPrompt);
    CallChatGPT(userPrompt);
    // GenerateSummaries(tempGPTAnswer);
    //showLoading(false);
}

//make ChatGPT prompt (User)
function NumericBulletList(texts) {
    let prompt = "";
    for (let i = 0; i < texts.length; i++) {
        prompt += (i + 1) + "- " + texts[i] + "\n";
    }
    return prompt;
}
// Function to escape HTML characters:
const escapeHtml = (unsafe) => {
    return unsafe.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;').replaceAll("'", '&#039;');
}

// ChatGPT API Call:
function CallChatGPT(prompt) {

    if (api_key.length == 0) {
        console.error("API Key is empty.");
        GenerateSummaries("");
        return;
    }


    msgs = [
        {
            "role": "system",
            
            "content": gptSystemInstruction
        },
        {
            "role": "user",
            "content": prompt
        }
    ];


    send_message = {
        model: model_name,
        messages: msgs,
        temperature: temprature,
        n: num_responses,
    };

    // Success function()
    success_behavior = function (reply_message) {
        showLoading(false);
        console.log("ChatGPT response:")
        console.log(reply_message)

        retMsg = ""

        // Actual message from ChatGPT:
        for (i = 0; i < reply_message.choices.length; i++) {

            curMsg = reply_message.choices[i].message.content;

            retMsg += curMsg
        }
        console.log(retMsg);

        const regex = /{([^}]+)}/g;
        const matches = retMsg.match(regex);
        const extractedStrings = matches ? matches.map(match => match.slice(1, -1)) : [];

        GenerateSummaries(JSON.parse("{" + extractedStrings[0] + "}"));
        init();
    };


    var ajaxTime = new Date().getTime();
    GenerateSummaries("");
    console.log("Call ChatGPT API");
    showLoading(true);

    $.ajax({
        type: "POST",
        url: openai_url,
        beforeSend: function (xhr) {
            xhr.setRequestHeader('Authorization', 'Bearer ' + api_key);
        },
        data: JSON.stringify(send_message),
        success: success_behavior,
        contentType: 'application/json; charset=utf-8',
        dataType: "json"
    }).done(function (data) {
        console.log("ChatGPT API Call Done");
        showLoading(false);
    }).fail(function (data) {
        console.error("ChatGPT API Call Failed");
        console.error(data);
        showLoading(false);
        GenerateSummaries("");
    });

};
function CallChatGPT_ShortReq(prompt, callbackFunc, instruction = gptSystemInstruction, modelName = model_name, returnJSON = true) {

    if (api_key.length == 0) {
        console.error("API Key is empty.");
        GenerateSummaries("");
        return;
    }

    msgs = [
        {
            "role": "system",
            "content": instruction
        },
        {
            "role": "user",
            "content": prompt
        }
    ];

    // These are other settings to send to OpenAI:
    send_message = {
        model: modelName,
        messages: msgs,
        temperature: temprature,
        n: num_responses,
    };

    // Success function()
    success_behavior = function (reply_message) {
        console.log("ChatGPT response:")
        console.log(reply_message)

        retMsg = ""

        for (i = 0; i < reply_message.choices.length; i++) {

            curMsg = reply_message.choices[i].message.content;

            retMsg += curMsg
        }
        console.log(retMsg);
        const regex = /{([^}]+)}/g;
        const matches = retMsg.match(regex);
        const extractedStrings = matches ? matches.map(match => match.slice(1, -1)) : [];
        if (returnJSON) callbackFunc(JSON.parse("{" + extractedStrings[0] + "}"));
        else callbackFunc(retMsg);
    };


    var ajaxTime = new Date().getTime();
    console.log("Call ChatGPT API, with this prompt:" + prompt);
    // This is the actual call to OpenAI
    $.ajax({
        type: "POST",
        url: openai_url,
        beforeSend: function (xhr) {
            xhr.setRequestHeader('Authorization', 'Bearer ' + api_key);
        },
        data: JSON.stringify(send_message),
        success: success_behavior,
        contentType: 'application/json; charset=utf-8',
        dataType: "json"
    }).done(function (data) {
        console.log("ChatGPT API Call Done");
    }).fail(function (data) {
        console.error("ChatGPT API Call Failed");
        console.error(data);
        callbackFunc(null);
    });

};
// Effect
function showLoading(enable) {
    if (enable) {
        $("#floating-processimg").show();
        $("#summary").addClass("blury");
    } else {
        $("#floating-processimg").delay(1000).fadeOut(1000);
        $("#summary").removeClass("blury");
    }
}

// UI
var isEditing = false;

function getFigurePath(figRef) {
    let path = figRef.match(/\d+/)[0];
    return figure_paths[path - 1];
}
function enlargeImage(event) {
    var thumbnail = event.target;
    var enlarged = document.getElementById("enlargedImg");
    let path = getFigurePath(thumbnail.innerHTML);
    if (path == undefined) {
        return;
    }
    enlarged.style.backgroundImage = "url('" + path + "')";

    enlarged.style.left = (event.clientX + 10) + "px";
    enlarged.style.top = (event.clientY + 10) + "px";
}

function shrinkImage() {
    var enlarged = document.getElementById("enlargedImg");
    enlarged.style.backgroundImage = "";
}


function toggleEnableDisableImage(event) {
    // if (isEditing) {
    //     inputFigureNumber(event);
    //     return;
    // }
    var thumbnail = event.target;
    if (thumbnail.classList.contains("enabled")) {
        thumbnail.classList.remove("enabled");
        thumbnail.classList.add("disabled");
    } else {
        thumbnail.classList.remove("disabled");
        thumbnail.classList.add("enabled");
    }
    event.stopImmediatePropagation();
}
function addListeneresToFigLabels() {
    // add mouse listeneres to all thumbnails in jquery
    $(".thumbnail").on("mouseover", enlargeImage);
    $(".thumbnail").on("mouseout", shrinkImage);
    $(".thumbnail").on("click", toggleEnableDisableImage);
}
function addTooltipToRefs() {
    //$(document).tooltip();
    // $(function () {
    //     $(".summarisedtext").each(function () {
    //         if (isEditing) {
    //             return null;
    //         }
    //         $(this).tooltip({
    //             show: {
    //                 effect: "slideDown",
    //                 delay: 250
    //             },
    //             position: {
    //                 my: "center bottom-10",
    //                 at: "center top"
    //             },
    //             items: "[data-ref]",
    //             content: function () {
    //                 return inputTexts[$(this).data('ref') - 1];
    //             }
    //         }
    //         );
    //     }
    //     );
    // });
}


// end ui
function substituteRefWithLink(figRef) {
    if (figRef == undefined) {
        return;
    }
    var sp = document.createElement("span");
    sp.innerHTML = "[Fig" + figRef.match(/\d+/)[0] + "]";
    if (getFigurePath(figRef) == undefined) {
        sp.classList.add("wrongpath");
    } else {
        sp.classList.add("thumbnail");
        sp.classList.add("enabled");
    }
    return sp.outerHTML;
}

function convertRefToTags() {
    makeSummarisedTextPlain();
    $(".summarisedtext").html(function () {
        return this.innerHTML.replace(/\[([Ff][Ii][Gg]\s?\d+)\]/g, function (match) {
            return substituteRefWithLink(match);
        });
    });
    addListeneresToFigLabels();
}
function makeSummarisedTextPlain() {
    $(".footnoteref").remove();
    $(".summarisedtext").html(function () {
        return this.textContent || this.innerText;
    });
}
function editReport() {
    if (isEditing) {
        isEditing = false;
        $(".summarisedtext").attr('contenteditable', 'false');
        $(".comment-text").attr('contenteditable', 'false');
        $(".summarisedtext").removeClass("highlight");
        $(".comment-text").removeClass("highlight");
        convertRefToTags();
        highlightCommentKeysInSummarisedText();
    } else {
        isEditing = true;
        $(".summarisedtext").attr('contenteditable', 'true');
        $(".comment-text").attr('contenteditable', 'true');
        $(".summarisedtext").addClass("highlight");
        $(".comment-text").addClass("highlight");
        makeSummarisedTextPlain();
    }
}

function printReport() {
    if (isEditing) {
        editReport();
    }
    //Check if all summarisedtext and comment-text are validated
    let allValidated = true;
    let remainingTexts =[];
    $(".summarisedtext").each(function () {
        if (!$(this).hasClass("validated")) {
            allValidated = false;
            // organ name
            let organName = $(this).parent().text().split(":")[0];
            remainingTexts.push(organName);
            //return false;
        }
    });
    $(".comment-text").each(function () {
        if (!$(this).hasClass("validated")) {
            allValidated = false;
            let keyWord = $(this).parent().text().split(":")[0];
            remainingTexts.push(keyWord);
            //return false;
        }
    });
    if (!allValidated) {
        let remainingTextsString = "";
        for (let text of remainingTexts) {
            text = text.trim();
            remainingTextsString += text + "<br/>";
        }
        Swal.fire({
            title: '',
            type: 'warning',
            icon: 'warning',
            html: 'Please validate ALL generated texts by AI before printing.<p>Remaining parts:<br/> ' + remainingTextsString+'</p>',
            confirmButtonText: 'OK'
        });
        return;
    }


    //var printContents = document.body.innerHTML;
    var originalContents = document.body.innerHTML;

    // modify the print contents
    $("#summary").prepend("<div style=\"direction:ltr;\"><h1><center>Autopsy Report</center></h1><h4>Printed at: " + new Date().toLocaleString() + "</h4><h4>Identity: Someone</h4><h4>Examiner: Pathologist 1</h4></div>");
    $("button").remove();
    $(".disabled").remove();
    $("#referencesarea").remove();
    $("#resize-bar").remove();
    $("#summary").css('width', '100%');
    $("#summary").css('margin', '10px');
    // $("#summary").css("margin-left", "6.25%");//"12.5%");
    // $("#summary").css("margin-right", "6.25%");//"12.5%");
    $("#summary").css("border", "1px solid black");
    $("#summary").css("border-radius", "10px");
    $("#structuredreport").css("max-height", "fit-content");
    $(".thumbnail").replaceWith(function () {
        return '<a href="' + getFigurePath(this.innerHTML) + '" target="_blank">' + this.innerHTML + '</a>';
    });
    //$('#summary').css('display', 'table');
    let reportBody = $('<div style="display:table;"></div>');
    // make report body the parent of template
    reportBody.append($('#template'));
    reportBody.append($('#commentsection'));

    $("#summary").append(reportBody);
    $('#template').css('display', 'table-cell');
    $('#template').css('width', '80%');
    $('#commentsection').css('display', 'table-cell');
    $('#commentsection').css('width', '20%');
    $('#commentsection').css('font-size', '9pt');
    $('#commentsection').css('border-left', '1px dashed lightgrey');
    $('#commentsection').css('padding-left', '5px');
    $('.comment').css('border', '1px solid lightgrey');
    $('.comment-text').css('color', 'grey');
    $('.comment-text').css('overflow', 'wrap');
    $('.comment-delete').remove();
    //blur window
    cleanCanvas();
    window.print();
    document.body.innerHTML = originalContents;
    init();
}


function outputReferences() {
    let textRefArea = $("#references");
    textRefArea.empty();
    let count = 1;
    for (const textRef of inputTexts) {
        textRefArea.append('<div class="textref" data-refid="' + count + '"><span draggable="true" class="refnumber">' + count + '</span> ' + textRef + '</div>');
        count++;
    }
}

function init() {
    addListeneresToFigLabels();
    addTooltipToRefs();
    outputReferences();
    makeTextAreasResizable();
    addSumTextListeners();
    canvasLineDrawingInit();
    dragDropRefFunc();
    applyCustomisedMenu();
    fetchVocabs();
}

function makeCommentElement(UID, term, description, validated = false) {
    let comment = document.createElement("div");
    term = term.charAt(0).toUpperCase() + term.slice(1);
    comment.classList.add("comment");
    comment.innerHTML = `<div class="comment-text` + (validated ? ` validated` : ``) + `" id="commentid-` + UID + `">${term}: ${description}</div><div class="comment-delete" onclick="deleteComment(this)">X</div>`;
    $('#commentsection').prepend(comment);
    return comment;
}
function fetchVocabs() {
    // read json file
    if (commentsMap.size != 0) return;
    fetch('/js/vocabs.json')
        .then(response => response.json())
        .then(data => {
            // Work with the JSON data here
            // Retrieve and insert key values into commentsMap
            let commentUID = 1;
            Object.entries(data).forEach(([key, value]) => {
                //if key exists in summarisedtext then add it to commentsMap
                if ($(".summarisedtext").text().includes(key)) {
                    let commentElement = makeCommentElement(commentUID++, key, value, true);
                    commentsMap.set(key, commentElement);
                }
            });

            editReport();
            editReport();
        })
        .catch(error => {
            // Handle any errors that occur during the process
            console.log(error);
        });
}
function applyCustomisedMenu() {
    $("#custom-menu").html(`
    
        <div id="menu-basicfunc">
        <span id="menu-check-p"><input type="checkbox" id="menu-check" /></span><span id="menu-cut">Cut</span> <span id="menu-copy">Copy</span> <span id="menu-paste">Paste</span>
        </div>
        <hr>
        <ul>
            <li id="menu-undo">Undo</li>
            <li id="menu-edit">Edit</li>
            <li id="menu-lessdetail">Less Details</li>
            <li id="menu-moredetail">More Details</li>
            <li id="menu-explain">Explain in Comments</li>
        </ul>`);

    $(document).on("contextmenu", function (e) {
        e.preventDefault();
        lastRightClickReceiver = $(e.target);
        const menu = $("#custom-menu");
        menu.css({ top: e.pageY + "px", left: e.pageX + "px" });
        if (isEditing) $("#menu-edit").text('Save');
        else $("#menu-edit").text('Edit');

        // disable edit option
        if (!isEditing) $("#menu-edit").addClass("disabled");
        $("#menu-check").prop('disabled', true);
        $("#menu-check").prop('checked', false);
        $("#menu-lessdetail").addClass("disabled");
        $("#menu-moredetail").addClass("disabled");
        if ($(e.target).is('.summarisedtext')) {
            // enable edit option
            $("#menu-check").prop('disabled', false);
            $("#menu-edit").removeClass("disabled");
            $("#menu-check").prop('checked', $(e.target).hasClass("validated"));
            $("#menu-lessdetail").removeClass("disabled");
            $("#menu-moredetail").removeClass("disabled");
        } else if ($(e.target).is('.comment-text')) {
            $("#menu-edit").removeClass("disabled");
            $("#menu-check").prop('disabled', false);
            $("#menu-check").prop('checked', $(e.target).hasClass("validated"));
        }
        $("#menu-copy").addClass("disabled");
        $("#menu-cut").addClass("disabled");
        $("#menu-paste").addClass("disabled");
        $("#menu-undo").addClass("disabled");

        if (window.getSelection().toString().length > 0) {
            $("#menu-copy").removeClass("disabled");
        }
        if (isEditing && ($(e.target).is('.summarisedtext') || $(e.target).is('.comment-text'))) {
            if (window.getSelection().toString().length > 0) $("#menu-cut").removeClass("disabled");
            $("#menu-paste").removeClass("disabled");
        }

        //undo redo options
        $("#menu-undo").text("Undo");
        if (undoMap.has($(e.target).attr('id'))) {
            $("#menu-undo").removeClass("disabled");
        }
        if (redoMap.has($(e.target).attr('id'))) {
            $("#menu-undo").text("Redo");
            $("#menu-undo").removeClass("disabled");
        }
        menu.show();
    });

    $(document).on("click", function (event) {
        $("#custom-menu").hide();
    });
    $(document).on("mousedown", function (event) {
        if (!$('#custom-menu').is(':hover')) $("#custom-menu").hide();
    });
    $(document).on('dblclick', function (event) {
        if (isEditing && !$(event.target).is('.summarisedtext') && !$(event.target).is('.comment-text')) {
            editReport();
        }
    });


    $("#custom-menu li, #custom-menu span").on("click", function (e) {
        const clickedOption = $(this).text();

        switch (clickedOption) {
            case "Cut":
                cutSelectedText();
                break;
            case "Copy":
                copySelectedText();
                break;
            case "Paste":
                pasteCopiedText(lastRightClickReceiver);
                break;
            case "Edit": case "Save":
                if (!$('#menu-edit').hasClass('disabled')) editReport();
                break;
            case "Undo":
                undoAIGenerated(lastRightClickReceiver);
                break;
            case "Redo":
                redoAIGenerated(lastRightClickReceiver);
                break;
            case "Check":
                printReport();
                break;
            case "Less Details":
                LessDetail(lastRightClickReceiver);
                break;
            case "More Details":
                MoreDetail(lastRightClickReceiver);
                break;
            case "Explain in Comments":
                ExplainInComments();
                break;
            default:
                if (lastRightClickReceiver.is('.summarisedtext') || lastRightClickReceiver.is('.comment-text')) {
                    if (lastRightClickReceiver.hasClass("validated")) lastRightClickReceiver.removeClass('validated');
                    else lastRightClickReceiver.addClass('validated');
                }
        }

        $("#custom-menu").hide();

        e.stopPropagation();
    });
}
function highlightCommentKeysInSummarisedText() {
    makeSummarisedTextPlain();
    convertRefToTags();
    let i = 1;
    commentsMap.forEach((value, key) => {
        $('.summarisedtext').each(function () {
            let foundPlace = $(this).text().toLowerCase().indexOf(key)
            if (foundPlace != -1) {
                let commentid = value.querySelector('.comment-text').id;
                let tooltipTxt = value.getElementsByClassName('comment-text')[0].innerText;
                //console.log("\ncommentId = "+commentid+" \ntooltipTxt = "+tooltipTxt);
                $(this).html($(this).html().substring(0, foundPlace) + `<span class="comment-key" tooltip="${tooltipTxt}">` + $(this).html().substring(foundPlace, foundPlace + key.length) + `</span><a class="footnoteref" href="#` + commentid + `">` + i + `</a>` + $(this).html().substring(foundPlace + key.length));
            }
        });
        i++;
    });
}
function ExplainInComments() {
    if (navigator.clipboard && window.getSelection) {
        let selectedText = window.getSelection().toString();
        if (selectedText.trim().length > 0) {
            // Refine selected text
            selectedText = selectedText.replace(/\n/g, " ");
            selectedText = selectedText.trim().toLowerCase();
            selectedText = selectedText.charAt(0).toUpperCase() + selectedText.slice(1);
            if (commentsMap.has(selectedText.toLowerCase())) return;
            console.log("Add comment about \"" + selectedText + "\"");

            let comment = document.createElement("div");
            comment.classList.add("comment");
            comment.innerHTML = `<div class="comment-text">${selectedText}: "<img src=\"img/loading.gif\"  height=\"10px\">"</div><div class="comment-delete" onclick="deleteComment(this)">X</div>`;
            $("#commentsection").prepend(comment);
            // Call GPT to explain it
            askGPTtoExplain(selectedText, comment);
        }
    }
}
function askGPTtoExplain(term, outputDiv) {
    let instruction = "You are a medical assistant, specializing in explaining medical terms to laymen.\n"
    let prompt = "Explain term \"" + term + "\". Do not explain a lot, just explain the term without introduction and conclusion.";
    let callbackFunc = function (data, error = false) {
        if (error) {
            outputDiv.remove();
            return;
        }

        let retValue = data;
        if (retValue != null && retValue != undefined && retValue != "" && retValue.toLowerCase().trim() != "null") {
            // Update comment with the answer
            outputDiv.innerHTML = `<div class="comment-text" id="commentid-` + (commentUID++) + `">${term}: ${retValue}</div><div class="comment-delete" onclick="deleteComment(this)">X</div>`;
            commentsMap.set(term.trim().toLowerCase(), outputDiv);
            editReport();
            editReport();
        } else {
            outputDiv.remove();
        }
    };
    //fakeCallGPT_ShortReq(prompt, callbackFunc, instruction, gpt3modelname, false);
    CallChatGPT_ShortReq(prompt, callbackFunc, instruction, gpt3modelname, false);
}
function fakeCallGPT_ShortReq(prompt, callbackFunc, instruction, gpt3modelname, jsonformat) {
    setTimeout(() => { callbackFunc(prompt + "means ...", false); }, 1000);
}
function deleteComment(elem) {
    let comment = elem.parentNode;
    let key;
    if ((key = findCommentKeyInMap(comment)) === "") return;
    commentsMap.get(key).remove();
    commentsMap.delete(key);
}
function findCommentKeyInMap(comment) {
    let key = "";
    commentsMap.forEach((value, key1) => {
        if (value == comment) {
            key = key1;
        }
    });
    return key;
}
function LessDetail(element) {
    element = element[0];
    let instruction = `You are a medical assistant, with speciality in summarising medical texts and respond in JSON file.\n
    The JSON file is just one key-value pair, where the key is "causes" and the value is the summarised findings from the prompt. Keep the [figx] references intact. summarise as much as you can and keep less details."\n
    `
    let textData = "Summarise this:" + element.innerText;
    let mainHTML = element.innerHTML;
    let mainOuterHTML = element.outerHTML;
    let targetElement = element;
    let callbackFunc = function (data, error = false) {
        let retValue = data["causes"];
        if (retValue != null && retValue != undefined && retValue != "" && retValue.toLowerCase().trim() != "null") {
            addToUndo(targetElement.getAttribute('id'), mainOuterHTML);
            targetElement.innerText = retValue;
            targetElement.classList.remove("validated");
            convertRefToTags();
        } else {
            targetElement.innerHTML = mainHTML;
            convertRefToTags();

            if (error) {
                Swal.fire({
                    title: 'Error!',
                    icon: 'error',
                    text: 'Error in operation, try again!',
                    confirmButtonText: 'OK'
                });
            }
        }
        init();
        cleanCanvas();
    };
    targetElement.innerHTML = "<img src=\"img/loading.gif\"  height=\"10px\">";
    CallChatGPT_ShortReq(textData, callbackFunc, instruction);
}
function MoreDetail(element) {
    element = element[0];
    let textData = "";
    let refs = element.getAttribute('data-ref');
    //console.log(refs);
    if (refs === undefined || refs == null || refs.length === 0) return;
    let refIDs = refs.split(',');
    //console.log(refIDs);
    refIDs.forEach(refid => {
        let refElement = document.querySelector('.textref[data-refid="' + refid + '"]');
        //console.log(refElement + "for "+'.textref[data-refid="' + refid + '"]');
        if (refElement != null && refElement != undefined) {
            textData += refElement.innerText + "\n";
        }
    });

    let mainHTML = element.innerHTML;
    let mainOuterHTML = element.outerHTML;
    let targetElement = element;
    let partTitle = targetElement.parentElement.innerText;
    partTitle = partTitle.replace(/&nbsp;/g, '').replace(/^\s+/g, '');
    partTitle = partTitle.substring(0, partTitle.indexOf(":"));
    let instruction = `You are a medical assistant, with speciality in summarising medical texts about "` + partTitle + `" and respond in JSON file.\n
        The JSON file is just one key-value pair, where the key is "causes" and the value is the summarised findings from the prompt. Make sure to keep references [figx] intact or put all of them again at the end of the findings. Add a bit more details from prompt compared to the text below:"\n
        `+ element.innerText;
    let callbackFunc = function (data, error = false) {
        let retValue = data["causes"];
        if (retValue != null && retValue != undefined && retValue != "" && retValue.toLowerCase().trim() != "null") {
            addToUndo(targetElement.getAttribute('id'), mainOuterHTML);
            targetElement.innerText = retValue;
            targetElement.classList.remove("validated");
            convertRefToTags();
        } else {
            targetElement.innerHTML = mainHTML;
            convertRefToTags();

            if (error) {
                Swal.fire({
                    title: 'Error!',
                    icon: 'error',
                    text: 'Error in operation, try again!',
                    confirmButtonText: 'OK'
                });
            }
        }
        init();
        cleanCanvas();
    };
    targetElement.innerHTML = "<img src=\"img/loading.gif\"  height=\"10px\">";
    CallChatGPT_ShortReq(textData, callbackFunc, instruction);
}
function undoAIGenerated(element) {
    if (!element.is('.summarisedtext')) return;
    let elUID = element.attr('id');
    if (undoMap.has(elUID)) {
        let undoText = undoMap.get(elUID);
        redoMap.set(elUID, element[0].outerHTML);
        element.replaceWith(undoText);
        undoMap.delete(elUID);
    }
    init();
}
function redoAIGenerated(element) {
    if (!element.is('.summarisedtext')) return;
    let elUID = element.attr('id');
    if (redoMap.has(elUID)) {
        let redoText = redoMap.get(elUID);
        undoMap.set(elUID, element[0].outerHTML);
        element.replaceWith(redoText);
        redoMap.delete(elUID);
    }
    init();
}
function addToUndo(elUID, htmlContent) {
    let element = $(`#${elUID}`);
    if (!element.is('.summarisedtext')) return;
    undoMap.set(elUID, htmlContent);
    redoMap.delete(elUID);
}
function copySelectedText() {
    // Check if the browser supports the Clipboard API
    if (navigator.clipboard && window.getSelection) {
        let selectedText = window.getSelection().toString();
        // Write the selected text to the clipboard
        navigator.clipboard.writeText(selectedText)
            .then(() => {
                //copiedText = selectedText;
            })
            .catch(err => {
                console.error('Error copying text:', err);
            });
    }
}
function pasteCopiedText(element) {

    element.on('paste', (event) => {
        navigator.clipboard.readText()
            .then(text => {
                let element = window.getSelection().anchorNode.parentNode;
                let startPos = window.getSelection().anchorOffset;
                let endPos = window.getSelection().anchorOffset + window.getSelection().toString().length;
                element.innerText = element.innerText.substring(0, startPos) + text + element.innerText.substring(endPos, element.innerText.length);
            })
            .catch(err => {
                console.error('Failed to read clipboard contents: ', err);
            });
        element.focus();
        event.preventDefault();
    });
    element.trigger("paste");
    element.off('paste');
    // // Check if the browser supports the Clipboard API
    // if (navigator.clipboard) {
    //     var clipboardData = event.clipboardData;
    //     var pastedText = clipboardData;//.getData("text/plain");
    //     console.log(pastedText);
    //     var startPos = element.selectionStart;
    //     var endPos = element.selectionEnd;
    //     element.value = element.value.substring(0, startPos) + pastedText + element.value.substring(endPos, element.value.length);
    //     element.selectionStart = startPos + pastedText.length;
    //     element.selectionEnd = startPos + pastedText.length;
    //     element.focus();
    // }
}
function cutSelectedText() {
    // Check if the browser supports the Clipboard API
    if (navigator.clipboard && window.getSelection) {
        let selectedText = window.getSelection().toString();
        // Write the selected text to the clipboard
        navigator.clipboard.writeText(selectedText)
            .then(() => {
                let element = window.getSelection().anchorNode.parentNode;
                let startPos = window.getSelection().anchorOffset;
                let endPos = window.getSelection().anchorOffset + window.getSelection().toString().length;
                element.innerText = element.innerText.substring(0, startPos) + element.innerText.substring(endPos, element.innerText.length);
            })
            .catch(err => {
                console.error('Error cutting text:', err);
            });

    } else {
        console.error('Your browser does not support the Clipboard API.');
    }
}
function addAddAreaToSumText() {
    let sumTexts = $(".summarisedtext");
    sumTexts.after('<span class="addarea"> <img class="addbutton" src="./img/blue-add-button-16.png"> </span>');

    document.querySelectorAll('.addarea').forEach(ref => {
        ref.addEventListener('drop', dropElementToAdd);
        ref.addEventListener('dragover', dragOverElement);
        ref.addEventListener('dragenter', dragEnterElement);
        ref.addEventListener('dragleave', dragLeaveElement);
    });
}

function dragStart(event) {
    let textData = inputTexts[event.target.innerText - 1];
    event.dataTransfer.setData("Text", textData);
    event.dataTransfer.setData("NewSource", event.target.innerText);
    addAddAreaToSumText();
}
function dragEnd(event) {
    if (event.dataTransfer.dropEffect === "none") {
        $('.addarea').remove();
    }
}
function dropElementToAdd(event) {
    event.preventDefault();
    let textData = event.dataTransfer.getData("Text");
    let newRef = event.dataTransfer.getData("NewSource");
    let targetElement = event.target.parentElement;

    document.querySelectorAll('.addarea').forEach(ref => {
        if (targetElement !== ref) {
            ref.remove();
        }
    });
    let partTitle = targetElement.parentElement.innerText;
    partTitle = partTitle.replace(/&nbsp;/g, '').replace(/^\s+/g, '');
    partTitle = partTitle.substring(0, partTitle.indexOf(":"));
    let instruction = `You are a medical assistant, analyse the provided report in the user prompt. Identify the causes of injuries, trauma, and diseases related to "` + partTitle + `" and summarise them in a JSON file.\n
    The JSON file is just one key-value pair, where the key is "causes" and the value is the summarised findings about "`+ partTitle + `" from the prompt. If you cannot find anything related, just set "Null" to the value."\n
    `;
    let prompt = "\"" + textData + "\"";
    console.log("instruction:" + instruction + "\n\n\nprompt:" + prompt);
    let mainHTML = event.target.innerHTML;
    console.log("dropped in addarea");
    let callbackFunc = function (data, error = false) {
        let retValue = data["causes"];
        if (retValue != null && retValue != undefined && retValue != "" && retValue.toLowerCase().trim() != "null") {
            event.target.setAttribute("data-ref", event.target.getAttribute("data-ref") + "," + newRef);
            let refsStr = event.target.getAttribute("data-ref");
            let refs = refsStr.split(",");
            let figsIncluded = [];
            for (const ref of refs) {
                let values = getValuesByKey(textToFigs, ref - 1);
                if (values != undefined) {
                    for (const figNum of values) {
                        if (!figsIncluded.includes(figNum)) {
                            figsIncluded.push(figNum);
                            retValue += "[Fig" + (figNum + 1) + "]";
                        }
                    }
                }
            }
            let newSpanElement = document.createElement("span");
            newSpanElement.setAttribute("class", "summarisedtext");
            newSpanElement.setAttribute("data-ref", newRef);
            newSpanElement.setAttribute("id", "sumtext" + (sumtextUID++));
            newSpanElement.innerHTML = retValue;
            targetElement.replaceWith(newSpanElement);

            convertRefToTags();
        } else {
            targetElement.innerHTML = '';
            // modern alert
            if (error) {
                Swal.fire({
                    title: 'Error!',
                    icon: 'error',
                    text: 'Error in operation, try again!',
                    confirmButtonText: 'OK'
                });
            } else {
                Swal.fire({
                    title: '',
                    text: 'No new findings for "' + partTitle + '" from this text!',
                    confirmButtonText: 'OK'
                });
            }
        }
        init();
        cleanCanvas();
        //connectWithBezier(lineSrc,lineDst);
    };
    targetElement.innerHTML += "<img src=\"img/loading.gif\"  height=\"10px\">";
    CallChatGPT_ShortReq(textData, callbackFunc, instruction);
}
function dropElementToMerge(event) {
    event.preventDefault();
    event.target.classList.remove("highlight");
    $('.addarea').remove();
    let textData = event.dataTransfer.getData("Text");
    let newRef = event.dataTransfer.getData("NewSource");
    let targetElement = event.target;
    // keep all text, just exclude the referneces in []
    let summarisedText = event.target.innerText;
    let parentsummarisedText = event.target.parentElement.innerText;
    parentsummarisedText = parentsummarisedText.replace(/&nbsp;/g, '').replace(/^\s+/g, '');
    parentsummarisedText = parentsummarisedText.substring(0, parentsummarisedText.indexOf(":"));

    summarisedText = summarisedText.replace(/\[([Ff][Ii][Gg]\s?\d+)\]/g, "");
    //"gpt-3.5-turbo";
    let instruction = `You are a medical assistant, analyse the provided report in the user prompt. Identify the causes of injuries, trauma, and diseases related to "` + parentsummarisedText + `" and summarise them in a JSON file.\n
    The JSON file is just one key-value pair, where the key is "causes" and the value is the summarised text. Merge the related findings from the user prompt with the default value below, but if you cannot find anything related, just set "Null" to the default value. The default value is:\n"`+ summarisedText + `".`;
    let prompt = "\"" + textData + "\"";
    console.log("instruction:" + instruction + "\n\n\nprompt:" + prompt);
    let mainHTML = event.target.innerHTML;
    let mainOuterHTML = event.target.outerHTML;
    let callbackFunc = function (data, error = false) {
        let retValue = data["causes"];
        if (retValue != null && retValue != undefined && retValue != "" && retValue.toLowerCase().trim() != "null") {
            addToUndo(targetElement.getAttribute('id'), mainOuterHTML);
            event.target.setAttribute("data-ref", event.target.getAttribute("data-ref") + "," + newRef);
            let refsStr = event.target.getAttribute("data-ref");
            let refs = refsStr.split(",");
            let figsIncluded = [];
            for (const ref of refs) {
                let values = getValuesByKey(textToFigs, ref - 1);
                if (values != undefined) {
                    for (const figNum of values) {
                        if (!figsIncluded.includes(figNum)) {
                            figsIncluded.push(figNum);
                            retValue += "[Fig" + (figNum + 1) + "]";
                        }
                    }
                }
            }
            targetElement.innerText = retValue;
            targetElement.classList.remove("validated");
            convertRefToTags();
        } else {
            targetElement.innerHTML = mainHTML;
            convertRefToTags();
            //alert("No new findings found for this text");
            // modern alert
            if (error) {
                Swal.fire({
                    title: 'Error!',
                    icon: 'error',
                    text: 'Error in operation, try again!',
                    confirmButtonText: 'OK'
                });
            } else {
                Swal.fire({
                    title: '',
                    text: 'No new findings for "' + parentsummarisedText + '" from this text!',
                    confirmButtonText: 'OK'
                });
            }
        }
        init();
        cleanCanvas();
        //connectWithBezier(lineSrc,lineDst);
    };
    targetElement.innerHTML = "<img src=\"img/loading.gif\"  height=\"10px\">";
    CallChatGPT_ShortReq(textData, callbackFunc, instruction);
}
function dragOverElement(event) {
    event.preventDefault();
}
function dragLeaveElement(event) {
    event.target.classList.remove("highlight");
}
function dragEnterElement(event) {
    event.preventDefault();
    event.target.classList.add("highlight");
}
function dragDropRefFunc() {
    document.querySelectorAll('.refnumber').forEach(ref => {
        ref.addEventListener('dragstart', dragStart);
        ref.addEventListener('dragend', dragEnd);
    });
    document.querySelectorAll('.summarisedtext').forEach(ref => {
        ref.addEventListener('drop', dropElementToMerge);
        ref.addEventListener('dragover', dragOverElement);
        ref.addEventListener('dragenter', dragEnterElement);
        ref.addEventListener('dragleave', dragLeaveElement);
    });
}

function canvasLineDrawingInit() {
    $("#drawingcanvas").attr("width", $(document).width());
    $("#drawingcanvas").attr("height", $(document).height());
    drawingCanvas = document.getElementById("drawingcanvas");
    ctx = drawingCanvas.getContext("2d");
    $('#referencesarea').scroll(function () {
        cleanCanvas();
        connectWithBezier(lineSrc, lineDst);
    });
    $('#summary').scroll(function () {
        cleanCanvas();
        connectWithBezier(lineSrc, lineDst);
    });
    $('#referencesarea').resize(function () {
        cleanCanvas();
        connectWithBezier(lineSrc, lineDst);
    });
    $('#summary').resize(function () {
        cleanCanvas();
        connectWithBezier(lineSrc, lineDst);
    });
    $(window).resize(function () {
        $("#drawingcanvas").attr("width", $(document).width());
        $("#drawingcanvas").attr("height", $(document).height());
        drawingCanvas = document.getElementById("drawingcanvas");
        ctx = drawingCanvas.getContext("2d");
    });
    $('.textref').hover(
        function () {
            connectRefToTexts($(this)[0]);
        }, function () {
            cleanCanvas();
            connectWithBezier(lineSrc, lineDst);
        });
}

function customisedQuerySelector(class_name, attribute_name, attribute_value) {
    let allElements = document.querySelectorAll('.' + class_name);
    let selectedElements = Array.from(allElements).filter(el => {
        let dataRef = el.getAttribute(attribute_name);
        let dataRefArray = (dataRef + '').split(',');//.map(Number);
        return dataRefArray.includes(attribute_value);
    });
    return selectedElements;
}

function connectRefToTexts(element) {
    let refID = element.getAttribute('data-refid');
    let summarisedTexts = customisedQuerySelector('summarisedtext', 'data-ref', refID);//document.querySelectorAll('.summarisedtext[data-ref="' + refID + '"]');

    connectWithBezier(lineSrc, lineDst);
    for (let sumTxt of summarisedTexts) {
        connectWithBezier(sumTxt, element, "dotted");
    }
}

function addSumTextListeners() {
    let hoverTimer;

    $('.summarisedtext').hover(
        function () {
            let $element = $(this);
            hoverTimer = setTimeout(function () {
                if ($element.is(':hover')) {
                    matchScrollRefToText($element[0]);
                    // Run your transition code here
                }
            }, 500);
        },
        function () {
            clearTimeout(hoverTimer);
        }
    );
}

function matchScrollRefToText(target) {
    if ($(".textref").removeClass("highlightedtextref").length !== 0) {
        cleanCanvas();
    };
    if (target.getAttribute('data-ref') == undefined) return;
    let refIDs = (target.getAttribute('data-ref') + "").split(",");

    if (refIDs == undefined) return;
    lineSrc = [];
    lineDst = [];
    let firstRef = null;
    for (let refID of refIDs) {
        let refText = $(".textref[data-refid=\"" + refID + "\"]");
        if (firstRef == null) {
            firstRef = refText;
            let references = $("#references");
            let referencesArea = $("#referencesarea");
            let textheight = refText.height();
            let visibleHeight = referencesArea.height();
            let targetTopPos = target.offsetTop;//target.offset().top;

            if (targetTopPos + textheight > visibleHeight * 0.9) {
                targetTopPos = visibleHeight * 0.9 - textheight;
            }

            let topOffset = references.offset().top - refText.offset().top - (refText.height() / 2) + targetTopPos;
            references.offset({ top: topOffset });
        }
        refText.addClass("highlightedtextref");

        lineDst.push(refText[0]);
    }
    lineSrc.push(target);


    let intervalId;
    firstRef.on('transitionstart', function () {

        intervalId = setInterval(function () {
            cleanCanvas();
            connectWithBezier(lineSrc, lineDst);
        }, 0.005);
        setTimeout(function () { clearInterval(intervalId) }, 520);
    });
}

function cleanCanvas() {

    ctx.clearRect(0, 0, drawingCanvas.width, drawingCanvas.height);
}

function connectWithBezier(elem1, elem2, style = "dashed") {
    if (elem1 == undefined || elem2 == undefined || elem1.length == 0 || elem2 == 0) return;

    if (elem1.length > 1 || elem2.length > 1) {
        for (let srcEl of elem1) {
            for (let dstEl of elem2) {
                connectWithBezier(srcEl, dstEl, style);
            }
        }
        return;
    } else {
        if (Array.isArray(elem1)) elem1 = elem1[0];
        if (Array.isArray(elem2)) elem2 = elem2[0];
    }
    ctx.strokeStyle = "grey";
    ctx.lineWidth = 2;
    switch (style) {
        case "dashed":
            ctx.setLineDash([5, 5]);
            ctx.strokeStyle = "darkorange";
            ctx.lineWidth = 2;
            break;
        case "dotted":
            ctx.setLineDash([2, 5]);
            break;
        default:
            ctx.setLineDash([]);
            break;
    }
    const rect1 = elem1.getBoundingClientRect();
    const rect2 = elem2.getBoundingClientRect();

    // Calculate the center points of the elements
    const x1 = rect1.x + rect1.width + 4;
    const y1 = rect1.y + rect1.height / 2;
    const x2 = rect2.x - 2;
    const y2 = rect2.y + rect2.height / 2;

    // Calculate control points for the bezier curve
    const cp1x = (x1 + x2) / 2;
    const cp1y = y1;
    const cp2x = (x1 + x2) / 2;
    const cp2y = y2;

    // Draw the bezier curve
    ctx.beginPath();
    ctx.moveTo(x1, y1);
    ctx.bezierCurveTo(cp1x, cp1y, cp2x, cp2y, x2, y2);

    ctx.stroke();
}
// function drawCurvedLine(src, dst, style) {
//     switch (style) {
//         case "dashed":
//             style = "stroke-dasharray: 5, 5;";
//             break;
//         case "dotted":
//             style = "stroke-dasharray: 1, 5;";
//             break;
//         default:
//             style = "";
//     }
//     //
//     let drawingCanvas = document.getElementById("drawingcanvas");
//     let ctx = drawingCanvas.getContext("2d");
//     ctx.clearRect(0, 0, drawingCanvas.width, drawingCanvas.height);
//     let srcPos = $(src).offset();
//     srcPos.left += $(src).width() + 2;
//     srcPos.top += $(src).height() / 2;
//     let dstPos = $(dst).offset()+$(dst).parent().offset();
//     dstPos.left -= 2;
//     dstPos.top += $(dst).height() / 2;

//     let controlX = (srcPos.left + dstPos.left) / 2;
//     let controlY = (srcPos.top + dstPos.top) / 2;

//     ctx.beginPath();
//     ctx.moveTo(srcPos.left, srcPos.top);
//     ctx.quadraticCurveTo(controlX, controlY, dstPos.left, dstPos.top);
//     ctx.strokeStyle = "black";
//     ctx.lineWidth = 2;
//     ctx.stroke();
// }

function makeTextAreasResizable() {
    var resizeBar = $('#resize-bar');
    var leftPane = $('#summary');
    var rightPane = $('#referencesarea');

    resizeBar.on('mousedown', function (event) {
        event.preventDefault(); // Prevent text selection while dragging
        $(document).on('mousemove', resizePanes);
        $(document).on('mouseup', stopResize);
    });

    function resizePanes(event) {
        var containerWidth = leftPane.parent().width();
        var xPos = event.pageX - leftPane.offset().left;

        // Restrict the resize bar movement within the container
        if (xPos > 0 && xPos < containerWidth) {
            leftPane.width(xPos);
            rightPane.width(containerWidth - xPos);
        }
    }

    function stopResize() {
        $(document).off('mousemove', resizePanes);
        $(document).off('mouseup', stopResize);
    }
}