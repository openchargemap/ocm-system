module.exports = function (grunt) {

    // Project configuration.
    grunt.initConfig({
        pkg: grunt.file.readJSON('package.json'),
        clean: ["build/"],

        concat: {
            options: {
                separator: ';',
            },
            dist: {
                src: ['build/OCM_UI_LocalisationResources.*.js', 'build/languageList.js'],
                dest: 'build/languagePack.js',
            },
        },
        uglify: {
            dist: {
                files: {
                    'build/languagePack.min.js': ['build/OCM_UI_LocalisationResources.*.js', 'build/languageList.js']
                }
            }
        },
        copy: {
            app: {
                files: [
                  { expand: true, flatten: true, src: 'build/languagePack.min.js', dest: '../App/js/Localisation/', filter: 'isFile' }, // includes files in path                  
                ]
            },
            web: {
                files: [
                  { expand: true, flatten: true, src: 'build/languagePack.min.js', dest: '../Website/OCM.MVC/Scripts/OCM/Localisation/', filter: 'isFile' }, // includes files in path                  
                ]    
            },
            map: {
                files: [
                  { expand: true, flatten: true, src: 'build/languagePack.min.js', dest: '../API/OCM.Net/OCM.API.Web/Widgets/Map/scripts/Localisation/', filter: 'isFile' }, // includes files in path                  
                ]
            }
        }
    });


    grunt.registerTask('transformJsonResourceFiles', 'Create JS variable from JSON.', function () {

        grunt.log.write('Converting JSON to js...').ok();

        var expandOptions = {};

        var fileList = grunt.file.expand("./src/*.json");
        var languageList = [];
        var languageIndex = 0;

        //for each language file in /src/*.json : transform into corresponding js file
        fileList.forEach(function (filepath) {
            var fileContent = grunt.file.read(filepath);
            var jsonContent = grunt.file.readJSON(filepath);

            if (jsonContent != null && jsonContent._langCode != null) {
                var outputContent = "var localisation_dictionary_" + jsonContent._langCode + " =" + fileContent + ";";
                var outputFilename = "./build/OCM_UI_LocalisationResources." + jsonContent._langCode + ".js";

                grunt.file.write("./build/OCM_UI_LocalisationResources." + jsonContent._langCode + ".js", outputContent);
                grunt.log.ok('File ' + outputFilename + ' created for language ' + jsonContent._langTitle);

                languageList[languageIndex] = jsonContent;
                languageIndex++;

            } else {
               // grunt.log.ok('Skipping  ' + filepath + '');
            }

        });

        //export language list js file
        var languageFileContent = "var languageList = [";
        languageList.forEach(function (language) {
            languageFileContent += '{"ID":"' + language._langCode + '","Title":"' + language._langTitle + '"},';
        });
        languageFileContent += "];";
        grunt.file.write("./build/languageList.js", languageFileContent);
    });


    grunt.loadNpmTasks('grunt-contrib-clean');
    grunt.loadNpmTasks('grunt-contrib-copy');
    grunt.loadNpmTasks('grunt-contrib-concat');
    grunt.loadNpmTasks('grunt-contrib-uglify');

    // Default task(s).
    grunt.registerTask('default', [
        'clean',
        'transformJsonResourceFiles',
        'uglify:dist',
        'copy:app',
        'copy:web',
        'copy:map'
    ]); //'concat:dist',

};