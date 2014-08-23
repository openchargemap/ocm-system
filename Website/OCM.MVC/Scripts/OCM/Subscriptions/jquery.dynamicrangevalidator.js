//http://stackoverflow.com/questions/7851035/mvc-unobtrusive-range-validation-of-dynamic-values
//provide custom js for custom range validator (one decimal value cannot exceed the value of another)

if ($.validator && $.validator.unobtrusive) {
    $.validator.unobtrusive.adapters.add('dynamicrange', ['minvalueproperty', 'maxvalueproperty'],
        function (options) {
            options.rules.dynamicrange = options.params; //['dynamicrange']
            if (options.message !== null) {
                $.validator.messages.dynamicrange = options.message;
            }
        }
    );

    $.validator.addMethod('dynamicrange', function (value, element, params) {

        var minValue = 1;
        //if input element exist for min value, use that value
        if ($('input[name="' + params.minvalueproperty + '"]').length) {
            minValue = parseFloat(parseFloat($('input[name="' + params.minvalueproperty + '"]').val()).toFixed(2));
        }

        var maxValue = parseFloat(parseFloat($('input[name="' + params.maxvalueproperty + '"]').val()).toFixed(2));

        var currentValue = parseFloat(parseFloat(value).toFixed(2));

        if (isNaN(minValue) || isNaN(maxValue) || isNaN(currentValue) || minValue >= currentValue || currentValue >= maxValue) {
            var message = $(element).attr('data-val-dynamicrange');
            $.validator.messages.dynamicrange = $.format(message, minValue, maxValue);
            return false;
        }
        return true;
    }, '');

}