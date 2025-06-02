let currentSlide: number = 0;
let slideInterval: number | undefined;
const slideDuration: number = 5000;
let isInitialized: boolean = false;

export function initializeSlider(): void {
    if (isInitialized) {
        return;
    }

    isInitialized = true;
    startSlideShow();
}

export function cleanupSlider(): void {
    if (!isInitialized) {
        return;
    }

    if (slideInterval) {
        clearInterval(slideInterval);
        slideInterval = undefined;
    }

    isInitialized = false;
}

function startSlideShow(): void {
    if (slideInterval) clearInterval(slideInterval);

    slideInterval = window.setInterval(() => {
        const slides = document.querySelectorAll('.slide');
        if (slides.length > 0) {
            nextSlide();
        } else {
            cleanupSlider();
        }
    }, slideDuration);
}

export function nextSlide(): void {
    const slides = document.querySelectorAll('.slide');
    if (slides.length === 0) return;

    setSlide((currentSlide + 1) % slides.length);
}

export function prevSlide(): void {
    const slides = document.querySelectorAll('.slide');
    if (slides.length === 0) return;

    const slideCount = slides.length;
    setSlide((currentSlide - 1 + slideCount) % slideCount);
}

export function setSlide(index: number): void {
    const slides = document.querySelectorAll('.slide');
    const dots = document.querySelectorAll('.dot');
    if (slides.length === 0 || dots.length === 0) return;

    slides.forEach(slide => {
        slide.classList.remove('active');
    });
    dots.forEach(dot => {
        dot.classList.remove('active');
    });

    slides[index].classList.add('active');
    dots[index].classList.add('active');

    currentSlide = index;

    startSlideShow();
}