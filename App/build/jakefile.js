//Open Charge Map App multi-platform build script
//requires NodeJS and 'Jake' build system
//originally derived from jake build example at https://github.com/drichard/mindmaps

/* Includes */
var fs = require("fs");
var path = require("path");
var wrench = require("wrench");
var UglifyJS = require("uglify-js");
var moment = require("moment");
var util = require("util");

/* Globals */
var buildDate = new Date();
var releaseVersion = "5.0.14_" + moment().format('YYYYMMDD');
var indexFileName = "index.html";
var srcDir = "../";
var buildDir = "../../../../builds/OCM.App";
var targetList = ["web", "mobile/Cordova"]; //, "mobile/Android", "mobile/WP7"
var scriptFilename = "app.script.min.js";
var scriptDir = "js/";
var regexCordovaSection = /<!--JS:Cordova:Begin-->([\s\S]*?)<!--JS:Cordova:End-->/;
var regexScriptSection = /<!-- JS:LIB:BEGIN -->([\s\S]*?)<!-- JS:LIB:END -->/;
var excludeFiles = [".gitignore", ".git", ".idea", "bin", "test", ".settings", "build", "obj", "res",
    ".project", "README.md", "*psd", "*.psd", "*libs", "CordovaNotes.txt", "*.bat", ".svn", "*.csproj*","*.md","*.config","*.user","*.profile.*"];

var mobileVersions = ["Android", "WP7", "iOS"];

/* Build Tasks */
console.log("Reading:" + srcDir + indexFileName + "::");

var indexFile = fs.readFileSync(srcDir + indexFileName, "utf8");
var scriptNames = [];

desc("Clean old build directory");
task("clean-dir", function () {
    if (fs.existsSync(buildDir)) {
        console.log("Deleting old bin directory");

        wrench.rmdirSyncRecursive(buildDir);
    }
});

desc("Create new directory");
task("create-dir", ["clean-dir"], function () {
    console.log("Creating new directory structure");

    if (!fs.existsSync(buildDir)) fs.mkdirSync(buildDir);
    if (!fs.existsSync(buildDir + "/mobile")) fs.mkdirSync(buildDir + "/mobile");
    targetList.forEach(
    function (trg) {
        fs.mkdirSync(buildDir + "/" + trg);
        fs.mkdirSync(buildDir + "/" + trg + "/" + scriptDir);
    });
});

desc("Minify scripts");
task("minify-js", function () {
    extractScriptNames();

    //minify and export scripts to each target
    targetList.forEach(
      function (trg) {
          minifyScripts(buildDir + "/" + trg + "/");
      });

    // find the scripts in index.html
    function extractScriptNames() {
        console.log("Extracting script file names from index.html");
        var regexScriptName = /<script src="(.*?)"><\/script>/g;

        var scriptSection = regexScriptSection.exec(indexFile)[1];

        // extract script names
        var match;
        while ((match = regexScriptName.exec(scriptSection)) != null) {
            var script = match[1];
            scriptNames.push(script);
        }
    }

    function minifyScripts(targetPath) {
        console.log("Minifying and concatenating scripts.");

        var regexMinifed = /min.js$/;
        var regexCopyright = /^\/\*![\s\S]*?\*\//m;
        var buffer = [];
        scriptNames.forEach(function (script) {
            var scriptFile = fs.readFileSync(srcDir + script, "utf8");
            var copyright = regexCopyright.exec(scriptFile);
            if (copyright) {
                buffer.push(copyright);
            }

            // check if file is already minified
            if (!regexMinifed.test(script)) {
                var ast = UglifyJS.parse(scriptFile);
                ast.figure_out_scope();
                var compressor = UglifyJS.Compressor();
                ast = ast.transform(compressor);

                ast.figure_out_scope();
                ast.compute_char_frequency();
                ast.mangle_names();

                scriptFile = ast.print_to_string();
            } else {
                console.log("> Skipping: " + script + " is already minified.");
            }

            buffer.push(scriptFile + ";");
        });

        var combined = buffer.join("\n");
        var scriptOutputPath = targetPath + scriptDir + scriptFilename;
        console.log("Combining all scripts into " + scriptOutputPath);
        fs.writeFileSync(scriptOutputPath, combined);
    }
});

desc("Use minified scripts in HTML");
task("use-min-js", ["minify-js"], function () {
    console.log("Replacing script files with minified version in index.html");
    indexFile = indexFile.replace(regexScriptSection, "<script src=\"js/"
        + scriptFilename + "\"></script>");
});

desc("Remove debug statements from HTML");
task("remove-debug", function () {
    console.log("Removing IF DEBUG statements in index.html");

    // remove debug code
    var regexDebug = /<!-- DEBUG -->[\s\S]*?<!-- \/DEBUG -->/gmi;
    indexFile = indexFile.replace(regexDebug, "");

    // insert production code
    var regexProduction = /<!-- PRODUCTION([\s\S]*?)\/PRODUCTION -->/gmi;
    indexFile = indexFile.replace(regexProduction, "$1");

    // remove all comments
    // var regexComments = /<!--[\s\S]*?-->/gmi;
    // indexFile = indexFile.replace(regexComments, "");
});

desc("Copy index.html");
task("copy-index", ["remove-debug"], function () { //"remove-debug","use-min-js"
    console.log("Copying index.html to build targets");

    //copy modified index.html to each target
    targetList.forEach(
      function (trg) {
          var finalIndexFile = indexFile;

          //apply search/replace modifications
          //append version to prevent script caching web build
          if (trg == "web") {
              console.log("Preventing cached scripts: " + trg);
              finalIndexFile = indexFile.replace(/(.js")/gmi, ".js?v=" + releaseVersion + "\"");
              finalIndexFile = finalIndexFile.replace(/(main.css")/gmi, "main.css?v=" + releaseVersion + "\"");

              //remove cordova references
              finalIndexFile = finalIndexFile.replace(regexCordovaSection, "<!--cordova/phonegap disabled-->");
          }
          //update version info
          finalIndexFile = finalIndexFile.replace(/(APPVERSION)/gmi, releaseVersion);

          finalIndexFile = finalIndexFile.replace(/(TARGETNAME)/gmi, trg.replace("mobile/", ""));

          //copy index.html to target
          console.log("Copying to Target: " + trg);

          fs.writeFileSync(buildDir + "/" + trg + "/" + indexFileName, finalIndexFile);
      });
});

desc("Copy all other files");
task("copy-files", ["minify-js"], function () {
    console.log("Copying all other files into /bin");

    function createExludeRegex() {
        // exclude files that get optimization treatment
        excludeFiles.push(indexFileName);

        //for now don't exclude minified scripts as other apps may have dependency
        //excludeFiles.push.apply(excludeFiles, scriptNames);

        // convert wildcard notation to proper regex
        // *foo.jpg becomes ^.*foo\.jpg$
        excludeFiles = excludeFiles.map(function (file) {
            file = file.replace(/\./g, "\\.").replace("*", ".*", "g");
            file = "^" + file + "$";
            return file;
        });

        return new RegExp(excludeFiles.join("|"));
    }

    var regexExcludeFiles = createExludeRegex();

    //copy current files to each build target folder
    targetList.forEach(
        function (trg) {
            copyFiles("", buildDir + "/" + trg + "/");
        });

    /**
     * Recursively copies all files that dont match the exclude filter from the
     * base directory to the publish directory.
     */
    function copyFiles(dir, targetDir) {
        var files = fs.readdirSync(srcDir + dir);
        files.forEach(function (file) {
            var currentDir = dir + file;
            if (!regexExcludeFiles.test(currentDir)) {
                var stats = fs.statSync(srcDir + currentDir);
                if (stats.isDirectory()) {
                    if (!fs.existsSync(targetDir + currentDir)) {
                        fs.mkdirSync(targetDir + currentDir);
                    }
                    copyFiles(currentDir + "/", targetDir);
                } else if (stats.isFile()) {
                    var contents = fs.readFileSync(srcDir + currentDir);
                    fs.writeFileSync(targetDir + currentDir, contents);
                }
            }
        });
    }
});

desc("Update cache manifest");
task("update-manifest", function () {
    /*
    // put new timestamp
    var fileDir = buildDir + "ocm.cache-manifest";
    var contents = fs.readFileSync(fileDir, "utf8");
    contents = contents.replace("{{timestamp}}", Date.now());
    fs.writeFileSync(fileDir, contents);
    */
});

desc("Build Target: Web");
task("build-web", ["create-dir", "concat", "copy-index", "copy-files", "update-manifest"], function () {
    console.log("Web Target built.");
});

desc("Build Target: Mobile");
task("build-mobile", ["create-dir", "copy-index", "copy-files", "update-manifest"], function () {
    console.log("Mobile Targets built.");
});

desc("Concatenates all html page templates files into body for use in index.html");
task('concat', function () {
    var files = fs.readdirSync(srcDir + "/views/");
    var output = "";
    files.forEach(function (file) {
        if (file.indexOf(".html") > 0) {
            var fileContent = fs.readFileSync(srcDir + "/views/" + file, "utf8");
            output += fileContent.replace(/^\uFEFF/, '') + "\r\n"; //concat file content, stripped BOM from UTF-8 Files
        }
    });

    indexFile = indexFile.replace("<!--INCLUDE:VIEWS-->", output);
});

desc("default");
task("default", ["build-web", "build-mobile"], function () {
    // build on default
});

/*
desc("Generate JSDoc");
task("generate-docs", function() {
  console.log("Creating project documentation");
  var exec = require('child_process').exec;
  var command = "docs/generate.sh";
  exec(command, function(error, stdout, stderr) {
    if (error !== null) {
      console.log('exec error: ' + error);
    } else {
      console.log("STDOUT:\n" + stdout);
      console.log("Created documentation");
    }

    if (stderr) {
      console.log("STDERR: " + stderr);
    }
  });
});
*/