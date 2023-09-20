var sectionNumber = 1;
// Generate Structured Report (without images, etc.)
function generateStructuredHTMLReport() {
    // Create HTML source code
    // var html = "<html><body><div style=\"padding:10px;\"><button onclick='window.close()'>Close</button></div>";

    // // add all elements
    // html += "<h1>Structured Report</h1>\
    // Internal Examination:\
    // <ul>\
    //     <li>Heart: Normal</li>\
    //     <li>Lungs: Normal</li>\
    //     <li>Abdomen: Normal</li>\
    //     <li>Extremities: Normal</li>\
    //     </ul>\
    //     External Examination:\
    //     <ul>\
    //         <li>Head: Normal</li>\
    //         <li>Neck: Normal</li>\
    //         <li>Chest: Normal</li>\
    //         </ul>";

    // html += "</body></html>";

    let figurePaths = [];
    $(".subContainer>img").each(function () {
        figurePaths.push($(this).attr("src"));
    });
    let textToFigs = [];
    let inputTexts = [];
    let transcriptionID = 0;
    $(".voice_transcript").each(function () {
        inputTexts.push($(this).text());
        let tempTextToFigs =[];
        let sibling =  $(this).prev();
        while(sibling) {
            if(sibling.prop("tagName") !== "IMG") break;
            tempTextToFigs.push([transcriptionID, figurePaths.indexOf(sibling.attr("src"))]);
            sibling = sibling.prev();
        }
        tempTextToFigs.reverse();
        sibling =  $(this).next();
        while(sibling) {
            if(sibling.prop("tagName") !== "IMG") break;
            tempTextToFigs.push([transcriptionID, figurePaths.indexOf(sibling.attr("src"))]);
            sibling = sibling.next();
        }
        transcriptionID++;
        textToFigs.push(...tempTextToFigs);
    });
    
    let html = '<!DOCTYPE html>\
    <html lang="en">\
    \
    <head>\
        <meta charset="UTF-8">\
        <meta http-equiv="X-UA-Compatible" content="IE=edge">\
        <meta name="viewport" content="width=device-width, initial-scale=1.0">\
        <script src="https://code.jquery.com/jquery-3.6.3.min.js"\
            integrity="sha256-pvPw+upLPUjgMXY0G+8O0xUf+/Im1MZjXxxgOcBQBXU=" crossorigin="anonymous"></script>\
        <script src="https://code.jquery.com/ui/1.13.2/jquery-ui.min.js"></script>\
        <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>\
        <script src="js/generatedstructuredreport.js"></script>\
        \
        <link href="https://code.jquery.com/ui/1.13.2/themes/base/jquery-ui.css" rel="stylesheet" type="text/css" />\
        <link href="css/structuredreportstyle.css" rel="stylesheet" type="text/css" />\
        <title>Pathology Report</title>\
        <script>\
        var inputTexts = '+JSON.stringify(inputTexts)+';\
        var figure_paths = '+JSON.stringify(figurePaths)+';\
        var textToFigs = '+JSON.stringify(textToFigs)+';\
        </script>\
    </head>\
    \
    <body>\
    <canvas id="drawingcanvas"></canvas>\
        <div id="structuredreport">\
            <div id="summary">\
                <div>\
                    <button onclick="editReport()">Edit</button>\
                    <button onclick="printReport()">Print</button>\
                </div>\
                <div id="template"></div>\
                <div id="commentsection"></div>\
            </div>\
            <div id="resize-bar"></div>\
        <div id="referencesarea">\
            <div id="references"></div>\
        </div>\
        </div>\
    \
        <div id="enlargedImg" class="enlarged"></div>\
        <div id="floating-processimg">\
        <div class="processing">\
    </div>\
    <span class="processing-text">Processing<span>...</span></span>\
    </div>\
    <div id="custom-menu"></div>\
        <script>\
            analyzeAndWrite(inputTexts);\n\
            init();\n\
        </script>\
    </body>\
    </html>';
    let newTab = window.open();
    newTab.document.write(html);

    // // Create a Blob object
    // var blob = new Blob([html], { type: "text/html" });

    // // Create a URL object
    // var url = URL.createObjectURL(blob);

    // // Open the URL in a new tab
    // window.open(url, "_blank");
}
// Generate Full Report (including images, etc.)
function generateHTMLFullReport() {
    // Create HTML source code
    let html = '<html><head>\
    <script src="js/generatedfullreport.js"></script>\
    \
    <style>.edited {\
        background-color: lightyellow;\
      }</style>\
      </head>\
      <body>\
      <div style="padding:10px;">\
      <button onclick="closeWindow();">Close</button>\
      <button onclick="printDoc();">Print Report</button>\
      <button onclick="toggleEditableDoc();">Edit Report</button>\
      </div>';

    // add all elements
    sectionNumber = 1;
    for (const section of document.getElementsByClassName("dataContainer")) {
        html += extractHTMLFromSection(section.getElementsByClassName("subContainer")[0]);
        sectionNumber++;
    }

    html += "<script>//window.print();\n</script></body></html>";

    let newTab = window.open();
    newTab.document.write(html);
}

function extractHTMLFromSection(section) {
    let text = "<div contenteditable = \"false\"><h1>Section " + sectionNumber + "</h1>";
    // return any types of elements within the section
    let imgs = [];
    let imgTakers = [];
    console.log(section.children);
    let imgNumber = 1;
    for (const child of section.children) {
        if (child.classList.contains("voice_transcript")) {
            text += child.innerText;
        }
        if (child.tagName == "IMG") {
            text += " (see <a href=\"#Figure" + sectionNumber + "-" + imgNumber + "\">Figure " + sectionNumber + "." + imgNumber + "</a>) ";
            imgTakers.push(child.getAttribute("data-imgtaker"));
            imgs.push(child.src);
            imgNumber++;
        }
    }
    imgNumber = 1;
    imgs.forEach(function (img, index) {
        let imgTaker = imgTakers[index];
        text += '<figure>\
        <img src="'+ img + '" alt="Figure ' + sectionNumber + ',' + imgNumber + '" id="Figure' + sectionNumber + '-' + imgNumber + '">\
        <figcaption>Figure '+ sectionNumber + '.' + imgNumber + ', taken by ' + imgTaker + '.</figcaption>\
    </figure>';
        imgNumber++;
    });
    text += "</div>";
    return text;
}

