<script>
    // Thêm hiệu ứng tương tác
    document.addEventListener('DOMContentLoaded', function() {
        // Animation khi load trang
        const formGroups = document.querySelectorAll('.form-group');
        formGroups.forEach((group, index) => {
        group.style.opacity = '0';
    group.style.transform = 'translateY(20px)';
            setTimeout(() => {
        group.style.transition = 'all 0.6s ease';
    group.style.opacity = '1';
    group.style.transform = 'translateY(0)';
            }, 100 * (index + 1));
        });

    // Thêm hiệu ứng ripple cho button
    const button = document.querySelector('.btn-primary');
    button.addEventListener('click', function(e) {
            const ripple = document.createElement('span');
    const rect = this.getBoundingClientRect();
    const size = Math.max(rect.width, rect.height);
    const x = e.clientX - rect.left - size / 2;
    const y = e.clientY - rect.top - size / 2;

    ripple.style.width = ripple.style.height = size + 'px';
    ripple.style.left = x + 'px';
    ripple.style.top = y + 'px';
    ripple.style.position = 'absolute';
    ripple.style.borderRadius = '50%';
    ripple.style.background = 'rgba(255, 255, 255, 0.5)';
    ripple.style.transform = 'scale(0)';
    ripple.style.animation = 'ripple 0.6s linear';
    ripple.style.pointerEvents = 'none';

    this.appendChild(ripple);

            setTimeout(() => {
        ripple.remove();
            }, 600);
        });

    // Thêm CSS cho animation ripple
    const style = document.createElement('style');
    style.textContent = `
    @keyframes ripple {
        to {
        transform: scale(4);
    opacity: 0;
                }
            }
    `;
    document.head.appendChild(style);
    });
</script>