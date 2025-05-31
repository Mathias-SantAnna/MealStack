const ModalFix = {
    init: function() {
        $(document).on('hidden.bs.modal', function () {
            // Remove any leftover backdrops
            $('.modal-backdrop').remove();

            $('body').removeClass('modal-open');
            $('body').css('padding-right', '');

            if (!$('.modal.show').length) {
                $('body').removeAttr('style');
            }
        });

        // Prevent multiple backdrops
        $(document).on('show.bs.modal', '.modal', function () {
            const zIndex = 1040 + (10 * $('.modal:visible').length);
            $(this).css('z-index', zIndex);
            setTimeout(function() {
                $('.modal-backdrop').not('.modal-stack').css('z-index', zIndex - 1).addClass('modal-stack');
            }, 0);
        });

        console.log("ModalFix initialized");
    }
};

$(document).ready(function() {
    ModalFix.init();
});

window.ModalFix = ModalFix;