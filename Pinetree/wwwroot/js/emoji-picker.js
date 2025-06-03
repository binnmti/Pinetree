// Emoji picker functionality for Markdown editor

let currentEmojiResolve = null;

/**
 * Shows the emoji picker modal and returns the selected emoji
 * @param {string} emojiHtml - The HTML content for the emoji picker
 * @returns {Promise<string>} The selected emoji or empty string if cancelled
 */
window.showEmojiPicker = function (emojiHtml) {
    return new Promise((resolve) => {
        currentEmojiResolve = resolve;

        // Create modal backdrop
        const backdrop = document.createElement('div');
        backdrop.id = 'emoji-picker-backdrop';
        backdrop.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background-color: rgba(0, 0, 0, 0.5);
            z-index: 1050;
            display: flex;
            align-items: center;
            justify-content: center;
        `;        // Create modal dialog
        const modal = document.createElement('div');
        modal.id = 'emoji-picker-modal';
        modal.style.cssText = `
            background: white;
            border-radius: 8px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
            width: 90vw;
            max-width: 800px;
            height: 80vh;
            max-height: 600px;
            overflow: hidden;
            position: relative;
        `;

        // Create header with close button
        const header = document.createElement('div');
        header.style.cssText = `
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding: 10px 15px;
            border-bottom: 1px solid #eee;
            background: #f8f9fa;
        `;

        const title = document.createElement('h4');
        title.textContent = 'Select Emoji';
        title.style.cssText = 'margin: 0; color: #333;';

        const closeBtn = document.createElement('button');
        closeBtn.innerHTML = '&times;';
        closeBtn.style.cssText = `
            background: none;
            border: none;
            font-size: 24px;
            cursor: pointer;
            color: #666;
            padding: 0;
            width: 30px;
            height: 30px;
            display: flex;
            align-items: center;
            justify-content: center;
            border-radius: 4px;
        `;
        closeBtn.onmouseover = () => closeBtn.style.backgroundColor = '#e9ecef';
        closeBtn.onmouseout = () => closeBtn.style.backgroundColor = 'transparent';
        closeBtn.onclick = () => closeEmojiPicker('');

        header.appendChild(title);
        header.appendChild(closeBtn);

        // Create content area
        const content = document.createElement('div');
        content.innerHTML = emojiHtml;

        modal.appendChild(header);
        modal.appendChild(content);
        backdrop.appendChild(modal);

        // Close on backdrop click
        backdrop.onclick = (e) => {
            if (e.target === backdrop) {
                closeEmojiPicker('');
            }
        };

        // Close on Escape key
        const handleKeydown = (e) => {
            if (e.key === 'Escape') {
                closeEmojiPicker('');
                document.removeEventListener('keydown', handleKeydown);
            }
        };
        document.addEventListener('keydown', handleKeydown);

        document.body.appendChild(backdrop);

        // Focus the modal for keyboard navigation
        modal.tabIndex = -1;
        modal.focus();
    });
};

/**
 * Handles emoji selection from the picker
 * @param {string} emoji - The selected emoji
 */
window.selectEmoji = function (emoji) {
    closeEmojiPicker(emoji);
};

/**
 * Closes the emoji picker and resolves the promise
 * @param {string} emoji - The selected emoji or empty string if cancelled
 */
function closeEmojiPicker(emoji) {
    const backdrop = document.getElementById('emoji-picker-backdrop');
    if (backdrop) {
        backdrop.remove();
    }

    if (currentEmojiResolve) {
        currentEmojiResolve(emoji);
        currentEmojiResolve = null;
    }
}

/**
 * Filters emojis based on search query
 * @param {string} query - The search query
 */
window.filterEmojis = function (query) {
    const searchInput = document.getElementById('emoji-search');
    if (searchInput) {
        searchInput.value = query;
    }

    const allButtons = document.querySelectorAll('.emoji-picker-button');
    const allCategories = document.querySelectorAll('.emoji-category');
    
    if (!query || query.trim() === '') {
        // Show all emojis if no search query
        allButtons.forEach(button => {
            button.style.display = 'inline-block';
        });
        allCategories.forEach(category => {
            category.style.display = 'block';
        });
        return;
    }

    const searchTerm = query.toLowerCase().trim();
    let visibleCount = 0;

    // Hide all categories first
    allCategories.forEach(category => {
        category.style.display = 'none';
    });

    allButtons.forEach(button => {
        const emoji = button.textContent.trim();
        const keywords = getEmojiKeywords(emoji);
        
        // Check if search term matches any keyword
        const matches = keywords.some(keyword => 
            keyword.toLowerCase().includes(searchTerm)
        );

        if (matches) {
            button.style.display = 'inline-block';
            // Show the parent category
            const category = button.closest('.emoji-category');
            if (category) {
                category.style.display = 'block';
            }
            visibleCount++;
        } else {
            button.style.display = 'none';
        }
    });

    // If no matches found, show a message
    if (visibleCount === 0) {
        const noResults = document.getElementById('emoji-no-results');
        if (!noResults) {
            const message = document.createElement('div');
            message.id = 'emoji-no-results';
            message.style.cssText = `
                text-align: center;
                padding: 20px;
                color: #666;
                font-style: italic;
            `;
            message.textContent = 'No emojis found for "' + query + '"';
            
            const container = document.querySelector('.emoji-picker-content');
            if (container) {
                container.appendChild(message);
            }
        }
    } else {
        // Remove no results message if it exists
        const noResults = document.getElementById('emoji-no-results');
        if (noResults) {
            noResults.remove();
        }
    }
}

/**
 * Gets keywords associated with an emoji for search functionality
 * @param {string} emoji - The emoji character
 * @returns {Array<string>} Array of keywords
 */
function getEmojiKeywords(emoji) {
    const emojiKeywords = {
        '😀': ['grin', 'smile', 'happy', 'joy', 'cheerful'],
        '😃': ['smile', 'happy', 'joy', 'grin'],
        '😄': ['smile', 'happy', 'joy', 'laugh', 'grin'],
        '😁': ['grin', 'smile', 'happy', 'joy'],
        '😆': ['laugh', 'happy', 'smile', 'joy', 'lol'],
        '😅': ['laugh', 'happy', 'smile', 'sweat', 'relief'],
        '🤣': ['laugh', 'lol', 'rofl', 'funny', 'hilarious'],
        '😂': ['laugh', 'cry', 'tears', 'joy', 'funny', 'lol'],
        '🙂': ['smile', 'happy', 'slight'],
        '🙃': ['upside', 'down', 'silly', 'crazy'],
        '😉': ['wink', 'flirt', 'playful'],
        '😊': ['smile', 'happy', 'blush', 'pleased'],
        '😇': ['angel', 'innocent', 'halo', 'good'],
        '🥰': ['love', 'hearts', 'adore', 'crush'],
        '😍': ['love', 'heart', 'eyes', 'crush', 'adore'],
        '🤩': ['star', 'eyes', 'excited', 'amazing'],
        '😘': ['kiss', 'love', 'heart', 'romantic'],
        '😗': ['kiss', 'whistle', 'love'],
        '☺️': ['smile', 'happy', 'pleased'],
        '😚': ['kiss', 'love', 'closed', 'eyes'],
        '😙': ['kiss', 'love', 'whistle'],
        '🥲': ['tear', 'happy', 'sad', 'emotional'],
        '😋': ['yum', 'delicious', 'tongue', 'taste'],
        '😛': ['tongue', 'playful', 'silly'],
        '😜': ['tongue', 'wink', 'playful', 'silly'],
        '🤪': ['crazy', 'wild', 'silly', 'zany'],
        '😝': ['tongue', 'silly', 'playful', 'closed', 'eyes'],
        '🤑': ['money', 'rich', 'dollar', 'greedy'],
        '🤗': ['hug', 'embrace', 'love', 'care'],
        '🤭': ['oops', 'secret', 'whisper', 'giggle'],
        '🤫': ['shush', 'quiet', 'secret', 'silence'],
        '🤔': ['think', 'hmm', 'consider', 'ponder'],
        '🤐': ['zip', 'quiet', 'secret', 'silence'],
        '🤨': ['suspicious', 'doubt', 'question', 'raised', 'eyebrow'],
        '😐': ['neutral', 'meh', 'expressionless'],
        '😑': ['expressionless', 'meh', 'blank'],
        '😶': ['no', 'mouth', 'quiet', 'speechless'],
        '😏': ['smirk', 'sly', 'suggestive'],
        '😒': ['unamused', 'meh', 'disappointed'],
        '🙄': ['eye', 'roll', 'annoyed', 'whatever'],
        '😬': ['grimace', 'awkward', 'nervous'],
        '🤥': ['lie', 'pinocchio', 'nose'],
        '😔': ['sad', 'disappointed', 'down'],
        '😕': ['confused', 'sad', 'disappointed'],
        '🙁': ['sad', 'frown', 'disappointed'],
        '☹️': ['sad', 'frown', 'disappointed'],
        '😣': ['persevere', 'struggle', 'effort'],
        '😖': ['confounded', 'frustrated', 'annoyed'],
        '😫': ['tired', 'fed', 'up', 'frustrated'],
        '😩': ['weary', 'tired', 'frustrated'],
        '🥺': ['pleading', 'puppy', 'eyes', 'sad'],
        '😢': ['cry', 'sad', 'tear', 'upset'],
        '😭': ['cry', 'sob', 'tears', 'sad'],
        '😤': ['huff', 'annoyed', 'frustrated', 'steam'],
        '😠': ['angry', 'mad', 'annoyed'],
        '😡': ['angry', 'mad', 'rage', 'furious'],
        '🤬': ['swear', 'curse', 'angry', 'symbols'],
        '🤯': ['exploding', 'head', 'mind', 'blown'],
        '😳': ['flushed', 'embarrassed', 'surprised'],
        '🥵': ['hot', 'sweat', 'heat'],
        '🥶': ['cold', 'freeze', 'blue'],
        '😱': ['scream', 'scared', 'shocked'],
        '😨': ['fearful', 'scared', 'worried'],
        '😰': ['anxious', 'worried', 'sweat'],
        '😥': ['disappointed', 'relieved', 'sweat'],
        '😓': ['downcast', 'sweat', 'sad'],
        '🤗': ['hugging', 'hug', 'embrace'],
        '🤔': ['thinking', 'hmm', 'ponder'],
        '🙄': ['rolling', 'eyes', 'annoyed'],
        '😴': ['sleep', 'tired', 'zzz'],
        '😪': ['sleepy', 'tired', 'tear'],
        '😵': ['dizzy', 'dead', 'knocked', 'out'],
        '🤐': ['zipper', 'mouth', 'quiet'],
        '🥴': ['woozy', 'drunk', 'dizzy'],
        '🤢': ['nausea', 'sick', 'green'],
        '🤮': ['vomit', 'sick', 'throw', 'up'],
        '🤧': ['sneeze', 'sick', 'tissue'],
        '😷': ['mask', 'sick', 'medical'],
        '🤒': ['thermometer', 'sick', 'fever'],
        '🤕': ['bandage', 'hurt', 'injured'],
        '🤠': ['cowboy', 'hat', 'western'],
        '😎': ['cool', 'sunglasses', 'awesome'],
        '🤓': ['nerd', 'geek', 'glasses', 'smart'],
        '🧐': ['monocle', 'fancy', 'inspection'],
        // Add more emojis and their keywords as needed
        '❤️': ['heart', 'love', 'red'],
        '🧡': ['heart', 'orange', 'love'],
        '💛': ['heart', 'yellow', 'love'],
        '💚': ['heart', 'green', 'love'],
        '💙': ['heart', 'blue', 'love'],
        '💜': ['heart', 'purple', 'love'],
        '🖤': ['heart', 'black', 'love'],
        '🤍': ['heart', 'white', 'love'],
        '🤎': ['heart', 'brown', 'love'],
        '💔': ['broken', 'heart', 'sad', 'love'],
        '❣️': ['heart', 'exclamation', 'love'],
        '💕': ['hearts', 'love', 'pink'],
        '💞': ['hearts', 'love', 'revolving'],
        '💓': ['heart', 'beating', 'love'],
        '💗': ['heart', 'growing', 'love'],
        '💖': ['heart', 'sparkling', 'love'],
        '💘': ['heart', 'arrow', 'cupid', 'love'],
        '💝': ['heart', 'gift', 'present', 'love'],
        '🎉': ['party', 'celebration', 'confetti'],
        '🎊': ['party', 'celebration', 'confetti', 'ball'],
        '🎈': ['balloon', 'party', 'celebration'],
        '🎁': ['gift', 'present', 'box'],
        '🎂': ['birthday', 'cake', 'celebration'],
        '🍰': ['cake', 'dessert', 'sweet'],
        '🍕': ['pizza', 'food', 'italian'],
        '🍔': ['burger', 'hamburger', 'food'],
        '🍟': ['fries', 'french', 'food'],
        '🌭': ['hot', 'dog', 'food'],
        '🥪': ['sandwich', 'food'],
        '🌮': ['taco', 'mexican', 'food'],
        '🌯': ['burrito', 'wrap', 'food'],
        '🥙': ['flatbread', 'food'],
        '🥚': ['egg', 'food'],
        '🍳': ['cooking', 'egg', 'fried', 'food'],
        '🥓': ['bacon', 'food', 'meat'],
        '🥞': ['pancakes', 'food', 'breakfast'],
        '🧇': ['waffle', 'food', 'breakfast'],
        '🍞': ['bread', 'food'],
        '🥖': ['baguette', 'bread', 'french', 'food'],
        '🥨': ['pretzel', 'food'],
        '🧀': ['cheese', 'food'],
        '🥗': ['salad', 'green', 'healthy', 'food'],
        '🍿': ['popcorn', 'snack', 'movie', 'food'],
        '🍯': ['honey', 'sweet', 'food'],
        '🥛': ['milk', 'drink', 'white'],
        '☕': ['coffee', 'drink', 'hot'],
        '🍵': ['tea', 'drink', 'hot'],
        '🥤': ['drink', 'soda', 'cup'],
        '🍺': ['beer', 'alcohol', 'drink'],
        '🍻': ['beers', 'cheers', 'alcohol', 'drink'],
        '🍷': ['wine', 'alcohol', 'drink'],
        '🥂': ['champagne', 'cheers', 'celebration', 'drink'],
        '🍸': ['cocktail', 'martini', 'alcohol', 'drink'],
        '🍹': ['tropical', 'drink', 'cocktail'],
        '🍾': ['champagne', 'bottle', 'celebration'],
        '🐶': ['dog', 'puppy', 'pet', 'animal'],
        '🐱': ['cat', 'kitten', 'pet', 'animal'],
        '🐭': ['mouse', 'animal'],
        '🐹': ['hamster', 'pet', 'animal'],
        '🐰': ['rabbit', 'bunny', 'animal'],
        '🦊': ['fox', 'animal'],
        '🐻': ['bear', 'animal'],
        '🐼': ['panda', 'bear', 'animal'],
        '🐨': ['koala', 'animal'],
        '🐯': ['tiger', 'animal'],
        '🦁': ['lion', 'animal'],
        '🐮': ['cow', 'animal'],
        '🐷': ['pig', 'animal'],
        '🐸': ['frog', 'animal'],
        '🐵': ['monkey', 'animal'],
        '🙈': ['monkey', 'see', 'no', 'evil'],
        '🙉': ['monkey', 'hear', 'no', 'evil'],
        '🙊': ['monkey', 'speak', 'no', 'evil'],
        '🐒': ['monkey', 'animal'],
        '🐔': ['chicken', 'animal'],
        '🐧': ['penguin', 'animal'],
        '🐦': ['bird', 'animal'],
        '🐤': ['chick', 'baby', 'bird', 'animal'],
        '🐣': ['hatching', 'chick', 'bird', 'animal'],
        '🐥': ['chick', 'bird', 'animal'],
        '🦆': ['duck', 'bird', 'animal'],
        '🦅': ['eagle', 'bird', 'animal'],
        '🦉': ['owl', 'bird', 'animal'],
        '🦇': ['bat', 'animal'],
        '🐺': ['wolf', 'animal'],
        '🐗': ['boar', 'pig', 'animal'],
        '🐴': ['horse', 'animal'],
        '🦄': ['unicorn', 'horse', 'fantasy'],
        '🐝': ['bee', 'insect', 'honey'],
        '🐛': ['bug', 'insect', 'worm'],
        '🦋': ['butterfly', 'insect'],
        '🐌': ['snail', 'slow'],
        '🐞': ['ladybug', 'insect'],
        '🐜': ['ant', 'insect'],
        '🦟': ['mosquito', 'insect'],
        '🕷️': ['spider', 'insect'],
        '🦀': ['crab', 'seafood'],
        '🦞': ['lobster', 'seafood'],
        '🦐': ['shrimp', 'seafood'],
        '🐠': ['fish', 'tropical'],
        '🐟': ['fish'],
        '🐡': ['blowfish', 'fish'],
        '🦈': ['shark', 'fish'],
        '🐙': ['octopus', 'seafood'],
        '🌸': ['cherry', 'blossom', 'flower', 'pink'],
        '🌺': ['hibiscus', 'flower'],
        '🌻': ['sunflower', 'flower', 'yellow'],
        '🌹': ['rose', 'flower', 'red', 'love'],
        '🌷': ['tulip', 'flower'],
        '🌼': ['daisy', 'flower'],
        '🌾': ['wheat', 'grain'],
        '🍀': ['clover', 'luck', 'four', 'leaf'],
        '🍃': ['leaves', 'nature'],
        '🌿': ['herb', 'leaves', 'nature'],
        '🌱': ['seedling', 'plant', 'growth'],
        '🌲': ['tree', 'evergreen', 'nature'],
        '🌳': ['tree', 'nature'],
        '🌴': ['palm', 'tree', 'tropical'],
        '🌵': ['cactus', 'desert', 'plant'],
        '🍎': ['apple', 'fruit', 'red'],
        '🍊': ['orange', 'fruit'],
        '🍋': ['lemon', 'fruit', 'yellow'],
        '🍌': ['banana', 'fruit', 'yellow'],
        '🍉': ['watermelon', 'fruit'],
        '🍇': ['grapes', 'fruit'],
        '🍓': ['strawberry', 'fruit', 'red'],
        '🍈': ['melon', 'fruit'],
        '🍒': ['cherries', 'fruit', 'red'],
        '🍑': ['peach', 'fruit'],
        '🥭': ['mango', 'fruit'],
        '🍍': ['pineapple', 'fruit'],
        '🥥': ['coconut', 'fruit'],
        '🥝': ['kiwi', 'fruit'],
        '🍅': ['tomato', 'fruit', 'red'],
        '🍆': ['eggplant', 'vegetable', 'purple'],
        '🥑': ['avocado', 'fruit', 'green'],
        '🥦': ['broccoli', 'vegetable', 'green'],
        '🥬': ['lettuce', 'leafy', 'green', 'vegetable'],
        '🥒': ['cucumber', 'vegetable', 'green'],
        '🌶️': ['pepper', 'hot', 'spicy', 'red'],
        '🌽': ['corn', 'vegetable', 'yellow'],
        '🥕': ['carrot', 'vegetable', 'orange'],
        '🥔': ['potato', 'vegetable'],
        '🍠': ['sweet', 'potato', 'vegetable'],
        '⚽': ['soccer', 'football', 'sport', 'ball'],
        '🏀': ['basketball', 'sport', 'ball'],
        '🏈': ['american', 'football', 'sport', 'ball'],
        '⚾': ['baseball', 'sport', 'ball'],
        '🥎': ['softball', 'sport', 'ball'],
        '🎾': ['tennis', 'sport', 'ball'],
        '🏐': ['volleyball', 'sport', 'ball'],
        '🏉': ['rugby', 'football', 'sport', 'ball'],
        '🥏': ['frisbee', 'disc', 'sport'],
        '🎱': ['pool', 'billiards', '8', 'ball'],
        '🏓': ['ping', 'pong', 'table', 'tennis'],
        '🏸': ['badminton', 'sport'],
        '🥅': ['goal', 'net', 'sport'],
        '⛳': ['golf', 'flag', 'hole'],
        '🏑': ['field', 'hockey', 'sport'],
        '🏒': ['ice', 'hockey', 'sport'],
        '🥍': ['lacrosse', 'sport'],
        '🏹': ['archery', 'bow', 'arrow'],
        '🎣': ['fishing', 'pole'],
        '🥊': ['boxing', 'gloves', 'sport'],
        '🥋': ['martial', 'arts', 'karate'],
        '🎿': ['ski', 'snow', 'sport'],
        '🛷': ['sled', 'snow'],
        '🥌': ['curling', 'stone', 'sport'],
        '⛸️': ['ice', 'skate', 'skating'],
        '🛼': ['roller', 'skate'],
        '🛹': ['skateboard', 'sport'],
        '⛷️': ['skier', 'snow', 'sport'],
        '🏂': ['snowboard', 'snow', 'sport'],
        '🏌️': ['golf', 'sport'],
        '🏄': ['surf', 'surfing', 'wave'],
        '🚣': ['rowing', 'boat'],
        '🏊': ['swimming', 'swim'],
        '⛹️': ['basketball', 'player', 'sport'],
        '🏋️': ['weightlifting', 'gym', 'strength'],
        '🚴': ['cycling', 'bike', 'bicycle'],
        '🚵': ['mountain', 'biking', 'bicycle'],
        '🤸': ['cartwheel', 'gymnastics'],
        '🤼': ['wrestling', 'sport'],
        '🤽': ['water', 'polo', 'sport'],
        '🤾': ['handball', 'sport'],
        '🤹': ['juggling', 'skill'],
        '🧘': ['meditation', 'yoga', 'zen'],
        '🛀': ['bath', 'bathing', 'relax'],
        '🛌': ['sleeping', 'bed', 'rest'],
        '🏃': ['running', 'run', 'jog'],
        '🚶': ['walking', 'walk'],
        '🧍': ['standing', 'person'],
        '🧎': ['kneeling', 'kneel'],
        '👶': ['baby', 'infant'],
        '🧒': ['child', 'kid'],
        '👦': ['boy', 'child'],
        '👧': ['girl', 'child'],
        '🧑': ['person', 'adult'],
        '👨': ['man', 'male'],
        '👩': ['woman', 'female'],
        '🧓': ['older', 'person', 'elderly'],
        '👴': ['old', 'man', 'grandfather'],
        '👵': ['old', 'woman', 'grandmother'],
        // Transportation
        '🚗': ['car', 'automobile', 'vehicle'],
        '🚕': ['taxi', 'cab', 'vehicle'],
        '🚙': ['suv', 'vehicle', 'car'],
        '🚌': ['bus', 'vehicle', 'public'],
        '🚎': ['trolleybus', 'bus', 'vehicle'],
        '🏎️': ['race', 'car', 'formula', 'speed'],
        '🚓': ['police', 'car', 'cop'],
        '🚑': ['ambulance', 'medical', 'emergency'],
        '🚒': ['fire', 'truck', 'emergency'],
        '🚐': ['minibus', 'van', 'vehicle'],
        '🚚': ['truck', 'delivery', 'vehicle'],
        '🚛': ['truck', 'articulated', 'lorry'],
        '🚜': ['tractor', 'farm', 'vehicle'],
        '🏍️': ['motorcycle', 'bike', 'motorbike'],
        '🛵': ['scooter', 'vespa', 'moped'],
        '🚲': ['bicycle', 'bike', 'cycle'],
        '🛴': ['kick', 'scooter'],
        '🚁': ['helicopter', 'chopper'],
        '✈️': ['airplane', 'plane', 'flight'],
        '🛩️': ['small', 'airplane', 'plane'],
        '🛫': ['departure', 'takeoff', 'plane'],
        '🛬': ['arrival', 'landing', 'plane'],
        '🚀': ['rocket', 'space', 'launch'],
        '🛸': ['ufo', 'alien', 'flying', 'saucer'],
        '🚂': ['train', 'locomotive', 'steam'],
        '🚃': ['railway', 'car', 'train'],
        '🚄': ['high', 'speed', 'train', 'bullet'],
        '🚅': ['bullet', 'train', 'fast'],
        '🚆': ['train', 'electric'],
        '🚇': ['metro', 'subway', 'underground'],
        '🚈': ['light', 'rail', 'train'],
        '🚉': ['station', 'train'],
        '🚊': ['tram', 'trolley'],
        '🚝': ['monorail', 'train'],
        '🚞': ['mountain', 'railway', 'train'],
        '🚟': ['suspension', 'railway'],
        '🚠': ['mountain', 'cableway'],
        '🚡': ['aerial', 'tramway'],
        '⛴️': ['ferry', 'boat', 'ship'],
        '🛥️': ['motor', 'boat'],
        '🚤': ['speedboat', 'boat'],
        '⛵': ['sailboat', 'boat', 'sailing'],
        '🛶': ['canoe', 'kayak', 'boat'],
        '🚣': ['rowboat', 'rowing'],
        '🛳️': ['passenger', 'ship', 'cruise'],
        '⚓': ['anchor', 'ship', 'boat'],
        '🚢': ['ship', 'boat'],
        // Weather and nature
        '☀️': ['sun', 'sunny', 'bright', 'hot'],
        '🌤️': ['sun', 'small', 'cloud', 'partly'],
        '⛅': ['sun', 'behind', 'cloud', 'partly'],
        '🌥️': ['sun', 'behind', 'large', 'cloud'],
        '☁️': ['cloud', 'cloudy', 'overcast'],
        '🌦️': ['sun', 'behind', 'rain', 'cloud'],
        '🌧️': ['rain', 'cloud', 'rainy'],
        '⛈️': ['thunder', 'cloud', 'rain', 'storm'],
        '🌩️': ['cloud', 'lightning', 'storm'],
        '🌨️': ['cloud', 'snow', 'snowy'],
        '❄️': ['snowflake', 'snow', 'cold'],
        '☃️': ['snowman', 'snow', 'winter'],
        '⛄': ['snowman', 'snow', 'winter'],
        '🌬️': ['wind', 'face', 'blowing'],
        '💨': ['dash', 'wind', 'speed', 'fast'],
        '🌪️': ['tornado', 'storm', 'wind'],
        '🌫️': ['fog', 'misty'],
        '☂️': ['umbrella', 'rain'],
        '🌈': ['rainbow', 'colors', 'arc'],
        '⚡': ['lightning', 'electric', 'storm'],
        '🔥': ['fire', 'hot', 'flame'],
        '💧': ['water', 'drop', 'tear'],
        '🌊': ['ocean', 'wave', 'water'],
        '🏔️': ['mountain', 'snow', 'peak'],
        '🗻': ['mount', 'fuji', 'mountain'],
        '🏕️': ['camping', 'tent', 'outdoors'],
        '🏖️': ['beach', 'umbrella', 'sand'],
        '🏜️': ['desert', 'sand', 'hot'],
        '🏝️': ['desert', 'island', 'tropical'],
        '🏞️': ['national', 'park', 'nature'],
        '🌋': ['volcano', 'mountain', 'eruption'],
        '⭐': ['star', 'night', 'bright'],
        '🌟': ['star', 'glowing', 'bright'],
        '✨': ['sparkles', 'shiny', 'magic'],
        '🌠': ['shooting', 'star', 'meteor'],
        '🌙': ['crescent', 'moon', 'night'],
        '🌛': ['first', 'quarter', 'moon', 'face'],
        '🌜': ['last', 'quarter', 'moon', 'face'],
        '🌚': ['new', 'moon', 'face', 'dark'],
        '🌝': ['full', 'moon', 'face', 'bright'],
        '🌞': ['sun', 'face', 'bright'],
        // Objects and symbols
        '⌚': ['watch', 'time', 'apple'],
        '📱': ['mobile', 'phone', 'cell'],
        '📲': ['mobile', 'phone', 'call'],
        '💻': ['laptop', 'computer', 'pc'],
        '⌨️': ['keyboard', 'computer'],
        '🖥️': ['desktop', 'computer', 'monitor'],
        '🖨️': ['printer', 'print'],
        '🖱️': ['computer', 'mouse'],
        '🖲️': ['trackball', 'mouse'],
        '💽': ['computer', 'disk', 'minidisc'],
        '💾': ['floppy', 'disk', 'save'],
        '💿': ['optical', 'disk', 'cd'],
        '📀': ['dvd', 'disk'],
        '🧮': ['abacus', 'calculation'],
        '🎥': ['movie', 'camera', 'film'],
        '🎞️': ['film', 'frames', 'movie'],
        '📽️': ['film', 'projector', 'movie'],
        '🎬': ['clapper', 'board', 'movie'],
        '📺': ['television', 'tv'],
        '📻': ['radio', 'music'],
        '🎙️': ['studio', 'microphone'],
        '🎚️': ['level', 'slider', 'control'],
        '🎛️': ['control', 'knobs', 'mixer'],
        '🧭': ['compass', 'navigation'],
        '⏰': ['alarm', 'clock', 'time'],
        '⏲️': ['timer', 'clock'],
        '⏱️': ['stopwatch', 'time'],
        '⏳': ['hourglass', 'sand', 'time'],
        '⌛': ['hourglass', 'done', 'time'],
        '📡': ['satellite', 'antenna', 'space'],
        '🔋': ['battery', 'power'],
        '🔌': ['electric', 'plug', 'power'],
        '💡': ['light', 'bulb', 'idea'],
        '🔦': ['flashlight', 'torch', 'light'],
        '🕯️': ['candle', 'light', 'flame'],
        '🪔': ['diya', 'lamp', 'oil'],
        '🧯': ['fire', 'extinguisher', 'safety'],
        '🛢️': ['oil', 'drum', 'barrel'],
        '💸': ['money', 'wings', 'flying'],
        '💵': ['dollar', 'bill', 'money'],
        '💴': ['yen', 'bill', 'money'],
        '💶': ['euro', 'bill', 'money'],
        '💷': ['pound', 'bill', 'money'],
        '💰': ['money', 'bag', 'dollar'],
        '💳': ['credit', 'card', 'payment'],
        '🧾': ['receipt', 'bill', 'payment'],
        '💎': ['diamond', 'gem', 'precious'],
        '⚖️': ['balance', 'scale', 'justice'],
        '🧰': ['toolbox', 'tools', 'repair'],
        '🔧': ['wrench', 'tool', 'repair'],
        '🔨': ['hammer', 'tool', 'build'],
        '⚒️': ['hammer', 'pick', 'tools'],
        '🛠️': ['hammer', 'wrench', 'tools'],
        '⛏️': ['pick', 'mining', 'tool'],
        '🔩': ['nut', 'bolt', 'screw'],
        '⚙️': ['gear', 'settings', 'cog'],
        '🧱': ['brick', 'build', 'wall'],
        '⛓️': ['chain', 'link', 'metal'],
        '🧲': ['magnet', 'attraction'],
        '🔫': ['pistol', 'gun', 'weapon'],
        '💣': ['bomb', 'explosive'],
        '🧨': ['firecracker', 'dynamite'],
        '🪓': ['axe', 'wood', 'tool'],
        '🔪': ['knife', 'blade', 'cut'],
        '🗡️': ['sword', 'weapon', 'blade'],
        '⚔️': ['crossed', 'swords', 'battle'],
        '🛡️': ['shield', 'protection', 'defense'],
        '🚬': ['cigarette', 'smoking'],
        '⚰️': ['coffin', 'death', 'funeral'],
        '⚱️': ['funeral', 'urn', 'ashes'],
        '🏺': ['amphora', 'jar', 'pottery']
    };

    return emojiKeywords[emoji] || [emoji];
}

// Add CSS for emoji picker animations
const style = document.createElement('style');
style.textContent = `
    #emoji-picker-backdrop {
        animation: fadeIn 0.2s ease-out;
    }
    
    #emoji-picker-modal {
        animation: slideIn 0.3s ease-out;
    }
    
    @keyframes fadeIn {
        from { opacity: 0; }
        to { opacity: 1; }
    }
    
    @keyframes slideIn {
        from { 
            opacity: 0;
            transform: scale(0.9) translateY(-20px);
        }
        to { 
            opacity: 1;
            transform: scale(1) translateY(0);
        }
    }
    
    .emoji-picker-button:hover {
        background-color: #f0f0f0 !important;
        transform: scale(1.1);
    }
    
    .emoji-picker-button:active {
        transform: scale(0.95);
    }
`;
document.head.appendChild(style);
