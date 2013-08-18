module.exports = function(grunt) {

  // Project configuration.
  grunt.initConfig({
    pkg: grunt.file.readJSON('package.json'),
    json2js: {
		build: {
			 src: 'src/<%= pkg.name %>.json',
			 dest: 'build/<%= pkg.name %>.js'
		}
	}
  });


  grunt.registerTask('json2js', 'Create JS variable from JSON.', function() {
    grunt.log.write('Converting JSON to js...').ok();

    var fileList = this.files;
grunt.log.write(this.files.length).ok();
    fileList.forEach(function(filepath) {
	      // Log which file has changed, and how.
	      grunt.log.ok('File "' + filepath + '" ' + fileList[filepath] + '.');
    });
  });

  // Default task(s).
  grunt.registerTask('default', ['json2js']);

};