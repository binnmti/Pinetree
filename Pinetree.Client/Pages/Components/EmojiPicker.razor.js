// EmojiPicker.razor.ts - TypeScript for the EmojiPicker component
// This file provides JavaScript interop functionality for the EmojiPicker component
// Global cache variables for performance optimization
const emojiKeywordsCache = {};
/**
 * Gets keywords associated with an emoji for search functionality
 * @param emoji - The emoji character
 * @returns Array of keywords
 */
function getEmojiKeywords(emoji) {
    // Check cache first for better performance
    if (emojiKeywordsCache[emoji]) {
        return emojiKeywordsCache[emoji];
    }
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
        '😝': ['tongue', 'crazy', 'silly', 'playful'],
        '🤑': ['money', 'rich', 'dollar', 'cash'],
        '🤗': ['hug', 'happy', 'arms', 'embrace'],
        '🤭': ['oops', 'giggle', 'surprise', 'shock'],
        '🤫': ['quiet', 'silence', 'hush', 'secret'],
        '🤔': ['think', 'ponder', 'thought', 'confused'],
        '🤐': ['zip', 'silence', 'quiet', 'mouth', 'sealed'],
        '🤨': ['raised', 'eyebrow', 'suspicious', 'doubt'],
        '😐': ['neutral', 'blank', 'straight', 'face'],
        '😑': ['expressionless', 'blank', 'nothing'],
        '😶': ['speechless', 'no', 'mouth', 'blank'],
        '😏': ['smirk', 'sarcasm', 'witty', 'suggestive'],
        '😒': ['unamused', 'unimpressed', 'disapproval'],
        '🙄': ['roll', 'eyes', 'annoyed', 'whatever'],
        '😬': ['grimace', 'awkward', 'nervous', 'tense'],
        '🤥': ['lie', 'liar', 'pinocchio', 'dishonest'],
        '😌': ['relieved', 'content', 'relaxed', 'calm'],
        '😔': ['sad', 'pensive', 'disappointed', 'down'],
        '😪': ['sleepy', 'tired', 'drowsy', 'nap'],
        '🤤': ['drool', 'hungry', 'desire', 'want'],
        '😴': ['sleep', 'tired', 'snore', 'rest', 'dream', 'zzz'],
        '😷': ['mask', 'sick', 'ill', 'covid', 'doctor', 'medical'],
        '🤒': ['sick', 'fever', 'thermometer', 'ill'],
        '🤕': ['hurt', 'injured', 'bandage', 'pain'],
        '🤢': ['nausea', 'sick', 'green', 'vomit'],
        '🤮': ['vomit', 'sick', 'throw', 'up', 'ill'],
        '🤧': ['sneeze', 'sick', 'cold', 'allergy'],
        '🥵': ['hot', 'heat', 'sweat', 'summer', 'warm'],
        '🥶': ['cold', 'freezing', 'ice', 'winter', 'chill'],
        '🥴': ['woozy', 'dizzy', 'intoxicated', 'confused'],
        '😵': ['dizzy', 'confused', 'spiral', 'dead'],
        '🤯': ['explode', 'mind', 'blown', 'shocked', 'surprise'],
        '🤠': ['cowboy', 'hat', 'western', 'rodeo'],
        '🥳': ['party', 'celebration', 'birthday', 'happy'],
        '😎': ['cool', 'sunglasses', 'awesome', 'rad'],
        '🤓': ['nerd', 'geek', 'glasses', 'smart'],
        '🧐': ['monocle', 'sophisticated', 'examine', 'curious'],
        '😕': ['confused', 'puzzled', 'unsure', 'uncertainty'],
        '😟': ['worried', 'concerned', 'nervous', 'anxious'],
        '🙁': ['sad', 'frown', 'unhappy', 'disappointed'],
        '☹️': ['sad', 'frown', 'unhappy', 'down'],
        '😮': ['wow', 'surprise', 'shock', 'open', 'mouth'],
        '😯': ['hushed', 'surprised', 'shocked', 'wow'],
        '😲': ['astonished', 'shocked', 'amazed', 'surprised'],
        '😳': ['flushed', 'blush', 'shy', 'embarrassed'],
        '🥺': ['pleading', 'puppy', 'eyes', 'cute'],
        '😦': ['frown', 'open', 'mouth', 'surprise', 'shock'],
        '😧': ['anguish', 'shock', 'surprised', 'scared'],
        '😨': ['fear', 'scared', 'terrified', 'afraid'],
        '😰': ['anxious', 'sweat', 'nervous', 'worried'],
        '😥': ['sad', 'disappointed', 'relieved', 'sweat'],
        '😢': ['cry', 'sad', 'tear', 'unhappy'],
        '😭': ['sob', 'cry', 'tears', 'sad', 'wail'],
        '😱': ['scream', 'fear', 'scared', 'terror', 'shock'],
        '😖': ['confounded', 'confused', 'frustrated', 'anxious'],
        '😣': ['persevere', 'struggle', 'stressed', 'effort'],
        '😞': ['disappointed', 'sad', 'upset', 'unhappy'],
        '😓': ['sweat', 'stress', 'hot', 'tired'],
        '😩': ['weary', 'tired', 'exhausted', 'frustration'],
        '😫': ['tired', 'exhausted', 'stress', 'weary'],
        '🥱': ['yawn', 'sleepy', 'tired', 'bored'],
        '😤': ['triumph', 'pride', 'steam', 'determined'],
        '😡': ['angry', 'mad', 'rage', 'hate'],
        '😠': ['angry', 'mad', 'annoyed', 'frustrated'],
        '🤬': ['curse', 'swear', 'angry', 'outrage', 'symbols'],
        '😈': ['devil', 'evil', 'horns', 'bad', 'mischief'],
        '👿': ['imp', 'devil', 'angry', 'evil', 'bad'],
        '💀': ['skull', 'death', 'dead', 'danger', 'poison'],
        '☠️': ['skull', 'crossbones', 'death', 'danger', 'poison'],
        '💩': ['poop', 'pile', 'crap', 'shit', 'funny'],
        '🤡': ['clown', 'circus', 'joke', 'funny', 'scary'],
        '👹': ['ogre', 'monster', 'japanese', 'demon', 'oni'],
        '👺': ['goblin', 'monster', 'japanese', 'demon', 'tengu'],
        '👻': ['ghost', 'spooky', 'halloween', 'scary', 'boo'],
        '👽': ['alien', 'ufo', 'space', 'extraterrestrial'],
        '👾': ['alien', 'monster', 'game', 'space', 'invader'],
        '🤖': ['robot', 'machine', 'ai', 'automated'],
        '🎃': ['pumpkin', 'halloween', 'jack-o-lantern'],
        '😺': ['cat', 'happy', 'smile', 'pet', 'animal'],
        '😸': ['cat', 'grin', 'happy', 'pet', 'animal'],
        '😹': ['cat', 'joy', 'tears', 'laugh', 'happy'],
        '😻': ['cat', 'heart', 'eyes', 'love', 'pet'],
        '😼': ['cat', 'smirk', 'mischief', 'pet', 'animal'],
        '😽': ['cat', 'kiss', 'love', 'pet', 'animal'],
        '🙀': ['cat', 'surprised', 'wow', 'shock', 'fear'],
        '😿': ['cat', 'cry', 'sad', 'tear', 'unhappy'],
        '😾': ['cat', 'grumpy', 'angry', 'mad', 'annoyed'],
        '🙈': ['monkey', 'see', 'evil', 'cover', 'eyes'],
        '🙉': ['monkey', 'hear', 'evil', 'cover', 'ears'],
        '🙊': ['monkey', 'speak', 'evil', 'cover', 'mouth'],
        '💋': ['kiss', 'lips', 'love', 'romance'],
        '💌': ['love', 'letter', 'heart', 'mail', 'romance'],
        '💘': ['heart', 'arrow', 'cupid', 'love', 'romance'],
        '💝': ['heart', 'ribbon', 'gift', 'love', 'present'],
        '💖': ['heart', 'sparkle', 'love', 'excited'],
        '💗': ['heart', 'growing', 'excited', 'nervous', 'love'],
        '💓': ['heart', 'beating', 'pulse', 'love', 'emotion'],
        '💞': ['heart', 'revolving', 'love', 'relationship'],
        '💕': ['hearts', 'love', 'like', 'affection'],
        '💟': ['heart', 'decoration', 'love', 'purple'],
        '❣️': ['heart', 'exclamation', 'love', 'decoration'],
        '💔': ['heart', 'broken', 'sad', 'breakup', 'grief'],
        '❤️': ['heart', 'love', 'like', 'red', 'romance'],
        '🧡': ['heart', 'orange', 'love', 'like'],
        '💛': ['heart', 'yellow', 'love', 'like', 'friendship'],
        '💚': ['heart', 'green', 'love', 'like', 'nature'],
        '💙': ['heart', 'blue', 'love', 'like', 'calm'],
        '💜': ['heart', 'purple', 'love', 'like', 'royalty'],
        '🤎': ['heart', 'brown', 'love', 'like'],
        '🖤': ['heart', 'black', 'dark', 'love', 'like'],
        '🤍': ['heart', 'white', 'love', 'like', 'pure'],
        '💯': ['hundred', '100', 'score', 'perfect'],
        '💢': ['anger', 'mad', 'symbol', 'annoyed'],
        '💥': ['collision', 'explode', 'impact', 'boom'],
        '💫': ['dizzy', 'star', 'sparkle', 'swirl'],
        '💦': ['sweat', 'drops', 'water', 'splash'],
        '💨': ['dash', 'wind', 'air', 'fast', 'run'],
        '🕳️': ['hole', 'empty', 'dark', 'depression'],
        '💣': ['bomb', 'explode', 'boom', 'explosion'],
        '💬': ['speech', 'balloon', 'bubble', 'talk', 'chat'],
        '👁️‍🗨️': ['eye', 'speech', 'bubble', 'witness'],
        '🗨️': ['speech', 'bubble', 'chat', 'left', 'talk'],
        '🗯️': ['anger', 'bubble', 'mad', 'speech', 'right'],
        '💭': ['thought', 'bubble', 'dream', 'thinking'],
        '💤': ['sleep', 'zzz', 'tired', 'snooze'],
        '👋': ['wave', 'hello', 'goodbye', 'hand'],
        '🤚': ['hand', 'raised', 'high', 'five', 'stop'],
        '🖐️': ['hand', 'fingers', 'spread', 'stop', 'high'],
        '✋': ['hand', 'raised', 'high', 'five', 'stop'],
        '🖖': ['vulcan', 'spock', 'star', 'trek', 'live', 'long'],
        '👌': ['ok', 'hand', 'perfect', 'agree'],
        '🤌': ['pinched', 'fingers', 'italian', 'what'],
        '🤏': ['pinch', 'small', 'little', 'tiny', 'amount'],
        '✌️': ['peace', 'victory', 'fingers', 'hand'],
        '🤞': ['fingers', 'crossed', 'luck', 'hopeful'],
        '🤟': ['love', 'you', 'hand', 'gesture', 'ily'],
        '🤘': ['rock', 'on', 'hand', 'horns', 'metal', 'music'],
        '🤙': ['call', 'me', 'hand', 'phone', 'gesture'],
        '👈': ['point', 'left', 'hand', 'direction', 'finger'],
        '👉': ['point', 'right', 'hand', 'direction', 'finger'],
        '👆': ['point', 'up', 'hand', 'direction', 'finger'],
        '🖕': ['middle', 'finger', 'rude', 'offensive'],
        '👇': ['point', 'down', 'hand', 'direction', 'finger'],
        '☝️': ['point', 'up', 'hand', 'direction', 'finger'],
        '👍': ['thumbs', 'up', 'approve', 'like', 'yes'],
        '👎': ['thumbs', 'down', 'disapprove', 'dislike', 'no'],
        '✊': ['fist', 'hand', 'punch', 'power', 'protest'],
        '👊': ['fist', 'bump', 'punch', 'hand', 'hit'],
        '🤛': ['fist', 'left', 'punch', 'bump', 'hand'],
        '🤜': ['fist', 'right', 'punch', 'bump', 'hand'],
        '👏': ['clap', 'applause', 'hands', 'praise'],
        '🙌': ['raise', 'hands', 'celebration', 'praise', 'hooray'],
        '👐': ['open', 'hands', 'hug', 'jazz', 'welcome'],
        '🤲': ['palms', 'up', 'together', 'pray', 'beg'],
        '🤝': ['handshake', 'agreement', 'deal', 'meeting'],
        '🙏': ['pray', 'please', 'hope', 'wish', 'thank'],
        '✍️': ['write', 'hand', 'pen', 'writing', 'signature'],
        '💅': ['nail', 'polish', 'manicure', 'cosmetics', 'beauty'],
        '🤳': ['selfie', 'camera', 'phone', 'photo'],
        '💪': ['muscle', 'biceps', 'strong', 'strength', 'flex'],
        '🦾': ['mechanical', 'arm', 'robot', 'prosthetic'],
        '🦿': ['mechanical', 'leg', 'robot', 'prosthetic'],
        '🦵': ['leg', 'kick', 'foot', 'limb'],
        '🦶': ['foot', 'toe', 'kick', 'stomp'],
        '👂': ['ear', 'hear', 'listen', 'sound'],
        '🦻': ['ear', 'hearing', 'aid', 'deaf', 'accessibility'],
        '👃': ['nose', 'smell', 'sniff', 'face'],
        '🧠': ['brain', 'smart', 'intelligent', 'mind'],
        '🫀': ['heart', 'organ', 'anatomical', 'cardiac'],
        '🫁': ['lungs', 'breath', 'organ', 'respiratory'],
        '🦷': ['tooth', 'teeth', 'dentist', 'mouth'],
        '🦴': ['bone', 'skeleton', 'joint', 'structure'],
        '👀': ['eyes', 'look', 'see', 'watch', 'stare'],
        '👁️': ['eye', 'look', 'see', 'watch', 'vision'],
        '👅': ['tongue', 'taste', 'lick', 'mouth'],
        '👄': ['mouth', 'lips', 'kiss', 'speak'],
        // Food & Drink Emojis
        '🍇': ['grapes', 'fruit', 'food', 'wine'],
        '🍈': ['melon', 'fruit', 'food'],
        '🍉': ['watermelon', 'fruit', 'food', 'summer'],
        '🍊': ['orange', 'tangerine', 'fruit', 'food'],
        '🍋': ['lemon', 'fruit', 'food', 'sour'],
        '🍌': ['banana', 'fruit', 'food', 'monkey'],
        '🍍': ['pineapple', 'fruit', 'food', 'tropical'],
        '🥭': ['mango', 'fruit', 'food', 'tropical'],
        '🍎': ['apple', 'red', 'fruit', 'food'],
        '🍏': ['apple', 'green', 'fruit', 'food'],
        '🍐': ['pear', 'fruit', 'food'],
        '🍑': ['peach', 'fruit', 'food'],
        '🍒': ['cherry', 'fruit', 'food', 'berry'],
        '🍓': ['strawberry', 'fruit', 'food', 'berry'],
        '🫐': ['blueberry', 'fruit', 'food', 'berry'],
        '🥝': ['kiwi', 'fruit', 'food'],
        '🍅': ['tomato', 'fruit', 'vegetable', 'food'],
        '🫒': ['olive', 'fruit', 'food', 'mediterranean'],
        '🥥': ['coconut', 'fruit', 'food', 'tropical'],
        '🥑': ['avocado', 'fruit', 'food', 'guacamole'],
        '🍆': ['eggplant', 'aubergine', 'vegetable', 'food'],
        '🥔': ['potato', 'vegetable', 'food'],
        '🥕': ['carrot', 'vegetable', 'food'],
        '🌽': ['corn', 'maize', 'vegetable', 'food'],
        '🌶️': ['pepper', 'hot', 'spicy', 'chili', 'food'],
        '🫑': ['pepper', 'bell', 'vegetable', 'food'],
        '🥒': ['cucumber', 'pickle', 'vegetable', 'food'],
        '🥬': ['leafy', 'green', 'vegetable', 'lettuce', 'food'],
        '🥦': ['broccoli', 'vegetable', 'food'],
        '🧄': ['garlic', 'food', 'spice', 'flavor'],
        '🧅': ['onion', 'food', 'vegetable', 'flavor'],
        '🍄': ['mushroom', 'fungi', 'food'],
        '🥜': ['peanuts', 'nuts', 'food', 'snack'],
        '🫘': ['beans', 'food', 'legume'],
        '🌰': ['chestnut', 'nut', 'food'],
        '🍞': ['bread', 'loaf', 'food', 'bakery'],
        '🥐': ['croissant', 'bread', 'food', 'french', 'bakery'],
        '🥖': ['baguette', 'bread', 'food', 'french', 'bakery'],
        '🫓': ['flatbread', 'food', 'pita', 'naan'],
        '🥨': ['pretzel', 'food', 'snack', 'twisted'],
        '🥯': ['bagel', 'food', 'bread', 'breakfast'],
        '🥞': ['pancakes', 'food', 'breakfast', 'flapjacks'],
        '🧇': ['waffle', 'food', 'breakfast'],
        '🧀': ['cheese', 'food', 'dairy'],
        '🍖': ['meat', 'food', 'bone', 'protein'],
        '🍗': ['poultry', 'leg', 'chicken', 'food', 'meat'],
        '🥩': ['steak', 'meat', 'food', 'beef'],
        '🥓': ['bacon', 'food', 'meat', 'breakfast'],
        '🍔': ['hamburger', 'burger', 'food', 'fast food'],
        '🍟': ['fries', 'chips', 'food', 'fast food'],
        '🍕': ['pizza', 'food', 'italian', 'slice'],
        '🌭': ['hot dog', 'food', 'sausage', 'fast food'],
        '🥪': ['sandwich', 'food', 'lunch'],
        '🌮': ['taco', 'food', 'mexican'],
        '🌯': ['burrito', 'food', 'mexican', 'wrap'],
        '🫔': ['tamale', 'food', 'mexican'],
        '🥙': ['stuffed', 'flatbread', 'food', 'falafel', 'gyro'],
        '🧆': ['falafel', 'food', 'mediterranean'],
        '🥚': ['egg', 'food', 'breakfast', 'protein'],
        '🍳': ['cooking', 'fried egg', 'food', 'breakfast'],
        '🥘': ['paella', 'food', 'pan', 'spanish'],
        '🍲': ['stew', 'soup', 'food', 'pot'],
        '🫕': ['fondue', 'food', 'cheese', 'swiss'],
        '🥣': ['bowl', 'cereal', 'food', 'breakfast'],
        '🥗': ['salad', 'food', 'healthy', 'green'],
        '🍿': ['popcorn', 'food', 'snack', 'movie'],
        '🧈': ['butter', 'food', 'dairy'],
        '🧂': ['salt', 'food', 'seasoning', 'condiment'],
        '🥫': ['canned', 'food', 'preserved'],
        '🍱': ['bento', 'box', 'food', 'japanese', 'lunch'],
        '🍘': ['rice', 'cracker', 'food', 'japanese'],
        '🍙': ['rice', 'ball', 'food', 'japanese', 'onigiri'],
        '🍚': ['cooked', 'rice', 'food', 'asian'],
        '🍛': ['curry', 'rice', 'food', 'indian', 'spicy'],
        '🍜': ['ramen', 'noodles', 'food', 'japanese', 'soup'],
        '🍝': ['spaghetti', 'pasta', 'food', 'italian'],
        '🍠': ['roasted', 'sweet potato', 'food'],
        '🍢': ['oden', 'food', 'japanese', 'skewer'],
        '🍣': ['sushi', 'food', 'japanese', 'fish', 'rice'],
        '🍤': ['fried', 'shrimp', 'food', 'tempura', 'seafood'],
        '🍥': ['fish', 'cake', 'food', 'japanese', 'naruto'],
        '🥮': ['moon', 'cake', 'food', 'chinese'],
        '🍡': ['dango', 'food', 'japanese', 'dessert', 'sweet'],
        '🥟': ['dumpling', 'food', 'asian', 'potsticker'],
        '🥠': ['fortune', 'cookie', 'food', 'chinese'],
        '🥡': ['takeout', 'box', 'food', 'chinese'],
        '🦀': ['crab', 'food', 'seafood', 'shellfish'],
        '🦞': ['lobster', 'food', 'seafood', 'shellfish'],
        '🦐': ['shrimp', 'food', 'seafood', 'shellfish'],
        '🦑': ['squid', 'food', 'seafood', 'calamari'],
        '🦪': ['oyster', 'food', 'seafood', 'shellfish'],
        '🍦': ['ice', 'cream', 'food', 'dessert', 'soft', 'sweet'],
        '🍧': ['shaved', 'ice', 'food', 'dessert', 'sweet'],
        '🍨': ['ice', 'cream', 'food', 'dessert', 'sweet'],
        '🍩': ['doughnut', 'donut', 'food', 'dessert', 'sweet'],
        '🍪': ['cookie', 'food', 'dessert', 'sweet'],
        '🎂': ['cake', 'birthday', 'food', 'dessert', 'sweet'],
        '🍰': ['cake', 'shortcake', 'food', 'dessert', 'sweet'],
        '🧁': ['cupcake', 'food', 'dessert', 'sweet'],
        '🥧': ['pie', 'food', 'dessert', 'sweet'],
        '🍫': ['chocolate', 'food', 'dessert', 'sweet'],
        '🍬': ['candy', 'food', 'dessert', 'sweet'],
        '🍭': ['lollipop', 'food', 'candy', 'dessert', 'sweet'],
        '🍮': ['custard', 'pudding', 'food', 'dessert', 'sweet'],
        '🍯': ['honey', 'food', 'sweet', 'bee'],
        '🍼': ['baby', 'bottle', 'milk', 'drink'],
        '🥛': ['milk', 'glass', 'drink', 'dairy'],
        '☕': ['coffee', 'drink', 'hot', 'caffeine'],
        '🫖': ['teapot', 'drink', 'hot'],
        '🍵': ['tea', 'drink', 'hot', 'green'],
        '🍶': ['sake', 'drink', 'alcohol', 'japanese'],
        '🍾': ['champagne', 'drink', 'alcohol', 'celebration'],
        '🍷': ['wine', 'drink', 'alcohol', 'glass'],
        '🍸': ['cocktail', 'drink', 'alcohol', 'martini'],
        '🍹': ['tropical', 'drink', 'cocktail', 'alcohol'],
        '🍺': ['beer', 'drink', 'alcohol', 'mug'],
        '🍻': ['beers', 'drink', 'alcohol', 'cheers'],
        '🥂': ['champagne', 'glasses', 'drink', 'alcohol', 'celebration'],
        '🥃': ['whiskey', 'drink', 'alcohol', 'tumbler'],
        '🫗': ['pouring', 'liquid', 'drink'],
        '🥤': ['cup', 'straw', 'drink', 'soda'],
        '🧋': ['bubble', 'tea', 'drink', 'boba'],
        '🧃': ['beverage', 'box', 'drink', 'juice'],
        '🧉': ['mate', 'drink', 'south american'],
        '🧊': ['ice', 'cube', 'cold', 'frozen'],
        // Animals & Nature
        '🐶': ['dog', 'puppy', 'animal', 'pet'],
        '🐱': ['cat', 'kitten', 'animal', 'pet'],
        '🐭': ['mouse', 'animal', 'rodent'],
        '🐹': ['hamster', 'animal', 'pet', 'rodent'],
        '🐰': ['rabbit', 'bunny', 'animal', 'easter'],
        '🦊': ['fox', 'animal', 'clever'],
        '🐻': ['bear', 'animal', 'teddy'],
        '🐼': ['panda', 'animal', 'china', 'bamboo'],
        '🐻‍❄️': ['polar', 'bear', 'animal', 'arctic', 'white'],
        '🐨': ['koala', 'animal', 'australia'],
        '🐯': ['tiger', 'animal', 'cat', 'stripes'],
        '🦁': ['lion', 'animal', 'cat', 'king'],
        '🐮': ['cow', 'animal', 'farm', 'milk'],
        '🐷': ['pig', 'animal', 'farm'],
        '🐽': ['pig', 'nose', 'animal'],
        '🐸': ['frog', 'animal', 'amphibian'],
        '🐵': ['monkey', 'animal', 'banana'],
        '🐒': ['monkey', 'animal', 'primate'],
        '🐔': ['chicken', 'animal', 'farm', 'bird'],
        '🐧': ['penguin', 'animal', 'bird', 'arctic'],
        '🐦': ['bird', 'animal', 'flying'],
        '🐤': ['baby', 'chick', 'bird', 'animal'],
        '🐣': ['hatching', 'chick', 'bird', 'animal'],
        '🐥': ['front', 'facing', 'baby', 'chick', 'bird'],
        '🦆': ['duck', 'bird', 'animal'],
        '🦅': ['eagle', 'bird', 'animal', 'flying'],
        '🦉': ['owl', 'bird', 'animal', 'wise'],
        '🦇': ['bat', 'animal', 'flying', 'vampire'],
        '🐺': ['wolf', 'animal', 'howl'],
        '🐗': ['boar', 'animal', 'pig'],
        '🐴': ['horse', 'animal', 'riding'],
        '🦄': ['unicorn', 'animal', 'magical', 'fantasy'],
        '🐝': ['bee', 'insect', 'honey', 'buzz'],
        '🪲': ['beetle', 'insect', 'bug'],
        '🐛': ['bug', 'insect', 'caterpillar'],
        '🦋': ['butterfly', 'insect', 'beautiful', 'transformation'],
        '🐌': ['snail', 'slow', 'animal'],
        '🐞': ['beetle', 'lady', 'bug', 'insect'],
        '🐜': ['ant', 'insect', 'work'],
        '🪰': ['fly', 'insect', 'pest'],
        '🪱': ['worm', 'insect', 'earthworm'],
        '🦗': ['cricket', 'insect', 'chirp'],
        '🕷️': ['spider', 'insect', 'web'],
        '🕸️': ['spider', 'web', 'insect'],
        '🦂': ['scorpion', 'animal', 'sting'],
        '🦟': ['mosquito', 'insect', 'bite'],
        '🦠': ['microbe', 'bacteria', 'virus', 'germ'],
        '💐': ['bouquet', 'flowers', 'nature'],
        '🌸': ['cherry', 'blossom', 'flower', 'spring'],
        '💮': ['white', 'flower', 'nature'],
        '🏵️': ['rosette', 'flower', 'decoration'],
        '🌹': ['rose', 'flower', 'love', 'red'],
        '🥀': ['wilted', 'flower', 'sad'],
        '🌺': ['hibiscus', 'flower', 'tropical'],
        '🌻': ['sunflower', 'flower', 'yellow'],
        '🌼': ['daisy', 'flower', 'nature'],
        '🌷': ['tulip', 'flower', 'spring'],
        '🌱': ['seedling', 'plant', 'growth'],
        '🪴': ['potted', 'plant', 'house'],
        '🌲': ['evergreen', 'tree', 'nature'],
        '🌳': ['deciduous', 'tree', 'nature'],
        '🌴': ['palm', 'tree', 'tropical'],
        '🌵': ['cactus', 'plant', 'desert'],
        '🌾': ['sheaf', 'rice', 'plant'],
        '🌿': ['herb', 'plant', 'nature'],
        '☘️': ['shamrock', 'clover', 'luck', 'irish'],
        '🍀': ['four', 'leaf', 'clover', 'luck'],
        '🍃': ['leaf', 'fluttering', 'nature'],
        '🍂': ['fallen', 'leaves', 'autumn', 'nature'],
        '🍁': ['maple', 'leaf', 'autumn', 'canada'],
        // Travel & Places
        '🌍': ['earth', 'globe', 'europe', 'africa'],
        '🌎': ['earth', 'globe', 'americas'],
        '🌏': ['earth', 'globe', 'asia', 'australia'],
        '🌐': ['globe', 'world', 'internet'],
        '🗺️': ['world', 'map', 'geography'],
        '🗾': ['map', 'japan', 'country'],
        '🧭': ['compass', 'navigation', 'direction'],
        '🏔️': ['mountain', 'snow', 'capped'],
        '⛰️': ['mountain', 'nature'],
        '🌋': ['volcano', 'mountain', 'eruption'],
        '🗻': ['mount', 'fuji', 'mountain', 'japan'],
        '🏕️': ['camping', 'tent', 'nature'],
        '🏖️': ['beach', 'umbrella', 'sand'],
        '🏜️': ['desert', 'sand', 'hot'],
        '🏝️': ['desert', 'island', 'tropical'],
        '🏞️': ['national', 'park', 'nature'],
        '🏟️': ['stadium', 'sports', 'arena'],
        '🏛️': ['classical', 'building', 'government'],
        '🏗️': ['construction', 'building', 'crane'],
        '🧱': ['brick', 'building', 'construction'],
        '🪨': ['rock', 'stone', 'solid'],
        '🪵': ['wood', 'log', 'tree'],
        '🛖': ['hut', 'house', 'home'],
        '🏘️': ['houses', 'neighborhood', 'community'],
        '🏚️': ['derelict', 'house', 'abandoned'],
        '🏠': ['house', 'home', 'building'],
        '🏡': ['house', 'garden', 'home'],
        '🏢': ['office', 'building', 'work'],
        '🏣': ['japanese', 'post', 'office'],
        '🏤': ['post', 'office', 'european'],
        '🏥': ['hospital', 'medical', 'health'],
        '🏦': ['bank', 'money', 'finance'],
        '🏨': ['hotel', 'accommodation', 'travel'],
        '🏩': ['love', 'hotel', 'accommodation'],
        '🏪': ['convenience', 'store', 'shop'],
        '🏫': ['school', 'education', 'building'],
        '🏬': ['department', 'store', 'shopping'],
        '🏭': ['factory', 'industry', 'manufacturing'],
        '🏯': ['japanese', 'castle', 'building'],
        '🏰': ['castle', 'fairy', 'tale', 'european'],
        '💒': ['wedding', 'marriage', 'church'],
        '🗼': ['tokyo', 'tower', 'landmark'],
        '🗽': ['statue', 'liberty', 'new', 'york'],
        '⛪': ['church', 'religion', 'christianity'],
        '🕌': ['mosque', 'religion', 'islam'],
        '🛕': ['hindu', 'temple', 'religion'],
        '🕍': ['synagogue', 'religion', 'judaism'],
        '⛩️': ['shinto', 'shrine', 'religion', 'japan'],
        '🕋': ['kaaba', 'mecca', 'religion', 'islam'],
        '⛲': ['fountain', 'water', 'park'],
        '⛺': ['tent', 'camping', 'outdoor'],
        '🌁': ['foggy', 'weather', 'mist'],
        '🌃': ['night', 'city', 'stars'],
        '🏙️': ['cityscape', 'urban', 'buildings'],
        '🌄': ['sunrise', 'mountains', 'morning'],
        '🌅': ['sunrise', 'water', 'morning'],
        '🌆': ['cityscape', 'dusk', 'sunset'],
        '🌇': ['sunset', 'buildings', 'evening'],
        '🌉': ['bridge', 'night', 'city'],
        '♨️': ['hot', 'springs', 'onsen'],
        '🎠': ['carousel', 'horse', 'amusement'],
        '🎡': ['ferris', 'wheel', 'amusement'],
        '🎢': ['roller', 'coaster', 'amusement'],
        '💈': ['barber', 'pole', 'haircut'],
        '🎪': ['circus', 'tent', 'entertainment'],
        // Transportation
        '🚂': ['locomotive', 'train', 'transport'],
        '🚃': ['railway', 'car', 'train'],
        '🚄': ['high', 'speed', 'train'],
        '🚅': ['bullet', 'train', 'fast'],
        '🚆': ['train', 'transport', 'railway'],
        '🚇': ['metro', 'subway', 'underground'],
        '🚈': ['light', 'rail', 'transport'],
        '🚉': ['station', 'train', 'transport'],
        '🚊': ['tram', 'transport', 'streetcar'],
        '🚝': ['monorail', 'transport', 'elevated'],
        '🚞': ['mountain', 'railway', 'transport'],
        '🚋': ['tram', 'car', 'transport'],
        '🚌': ['bus', 'transport', 'public'],
        '🚍': ['oncoming', 'bus', 'transport'],
        '🚎': ['trolleybus', 'transport', 'electric'],
        '🚐': ['minibus', 'transport', 'van'],
        '🚑': ['ambulance', 'emergency', 'medical'],
        '🚒': ['fire', 'engine', 'emergency'],
        '🚓': ['police', 'car', 'emergency'],
        '🚔': ['oncoming', 'police', 'car'],
        '🚕': ['taxi', 'transport', 'cab'],
        '🚖': ['oncoming', 'taxi', 'transport'],
        '🚗': ['car', 'automobile', 'transport'],
        '🚘': ['oncoming', 'automobile', 'car'],
        '🚙': ['recreational', 'vehicle', 'rv'],
        '🛻': ['pickup', 'truck', 'transport'],
        '🚚': ['delivery', 'truck', 'transport'],
        '🚛': ['articulated', 'lorry', 'truck'],
        '🚜': ['tractor', 'farm', 'agriculture'],
        '🏎️': ['racing', 'car', 'formula', 'one'],
        '🏍️': ['motorcycle', 'racing', 'bike'],
        '🛵': ['motor', 'scooter', 'transport'],
        '🦽': ['manual', 'wheelchair', 'accessibility'],
        '🦼': ['motorized', 'wheelchair', 'accessibility'],
        '🛴': ['kick', 'scooter', 'transport'],
        '🚲': ['bicycle', 'bike', 'transport'],
        '🛺': ['auto', 'rickshaw', 'transport'],
        '🚁': ['helicopter', 'aircraft', 'flying'],
        '🛸': ['flying', 'saucer', 'ufo', 'alien'],
        '🚀': ['rocket', 'space', 'launch'],
        '🛰️': ['satellite', 'space', 'communication'],
        '💺': ['seat', 'chair', 'airplane'],
        '🛶': ['canoe', 'boat', 'water'],
        '⛵': ['sailboat', 'boat', 'water'],
        '🚤': ['speedboat', 'boat', 'water'],
        '🛥️': ['motor', 'boat', 'water'],
        '🚢': ['ship', 'boat', 'cruise'],
        '⚓': ['anchor', 'ship', 'nautical'],
        '⛽': ['fuel', 'pump', 'gas', 'station'],
        '🚧': ['construction', 'warning', 'roadwork'],
        '🚨': ['police', 'car', 'light', 'emergency'],
        '🚥': ['horizontal', 'traffic', 'light'],
        '🚦': ['vertical', 'traffic', 'light'],
        '🛑': ['stop', 'sign', 'traffic'],
        '🚏': ['bus', 'stop', 'transport'],
        // Activities & Sports
        '⚽': ['soccer', 'ball', 'football', 'sport'],
        '🏀': ['basketball', 'ball', 'sport'],
        '🏈': ['american', 'football', 'ball', 'sport'],
        '⚾': ['baseball', 'ball', 'sport'],
        '🥎': ['softball', 'ball', 'sport'],
        '🎾': ['tennis', 'ball', 'sport'],
        '🏐': ['volleyball', 'ball', 'sport'],
        '🏉': ['rugby', 'football', 'ball', 'sport'],
        '🥏': ['flying', 'disc', 'frisbee', 'sport'],
        '🎱': ['pool', '8', 'ball', 'billiards'],
        '🪀': ['yo', 'yo', 'toy', 'game'],
        '🏓': ['ping', 'pong', 'table', 'tennis'],
        '🏸': ['badminton', 'racquet', 'sport'],
        '🏒': ['ice', 'hockey', 'stick', 'sport'],
        '🏑': ['field', 'hockey', 'stick', 'sport'],
        '🥍': ['lacrosse', 'stick', 'sport'],
        '🏏': ['cricket', 'bat', 'ball', 'sport'],
        '🪃': ['boomerang', 'sport', 'throw'],
        '🥅': ['goal', 'net', 'sport'],
        '⛳': ['flag', 'hole', 'golf'],
        '🪁': ['kite', 'flying', 'wind'],
        '🏹': ['bow', 'arrow', 'archery'],
        '🎣': ['fishing', 'pole', 'hook'],
        '🤿': ['diving', 'mask', 'underwater'],
        '🥊': ['boxing', 'glove', 'fight'],
        '🥋': ['martial', 'arts', 'uniform'],
        '🎽': ['running', 'shirt', 'marathon'],
        '🛹': ['skateboard', 'sport', 'skating'],
        '🛷': ['sled', 'sledding', 'snow'],
        '⛸️': ['ice', 'skate', 'skating'],
        '🥌': ['curling', 'stone', 'sport'],
        '🎿': ['ski', 'skiing', 'snow'],
        '⛷️': ['skier', 'skiing', 'snow'],
        '🏂': ['snowboard', 'skiing', 'snow'],
        '🪂': ['parachute', 'skydiving', 'flying'],
        '🏋️': ['weight', 'lifting', 'exercise'],
        '🤼': ['wrestling', 'sport', 'fight'],
        '🤸': ['cartwheel', 'gymnastics', 'acrobatics'],
        '⛹️': ['basketball', 'player', 'sport'],
        '🤺': ['fencing', 'sword', 'sport'],
        '🤾': ['handball', 'player', 'sport'],
        '🏌️': ['golf', 'player', 'sport'],
        '🏇': ['horse', 'racing', 'jockey'],
        '🧘': ['meditation', 'yoga', 'peace'],
        '🏄': ['surfing', 'surfer', 'wave'],
        '🏊': ['swimming', 'swimmer', 'water'],
        '🤽': ['water', 'polo', 'sport'],
        '🚣': ['rowing', 'boat', 'water'],
        '🧗': ['climbing', 'rock', 'sport'],
        '🚵': ['mountain', 'biking', 'cycling'],
        '🚴': ['cycling', 'bicycle', 'sport'],
        '🏆': ['trophy', 'award', 'winner'],
        '🥇': ['gold', 'medal', 'first', 'place'],
        '🥈': ['silver', 'medal', 'second', 'place'],
        '🥉': ['bronze', 'medal', 'third', 'place'],
        '🏅': ['sports', 'medal', 'award'],
        '🎖️': ['military', 'medal', 'honor'],
        '🎗️': ['reminder', 'ribbon', 'awareness'],
        '🎫': ['ticket', 'admission', 'event'],
        '🎟️': ['admission', 'ticket', 'event'],
        '🤹': ['juggling', 'performance', 'skill'],
        '🎭': ['performing', 'arts', 'theater'],
        '🩰': ['ballet', 'shoes', 'dance'],
        '🎨': ['artist', 'palette', 'painting'],
        '🎬': ['clapper', 'board', 'movie'],
        '🎤': ['microphone', 'singing', 'music'],
        '🎧': ['headphone', 'music', 'listening'],
        '🎼': ['musical', 'score', 'music'],
        '🎵': ['musical', 'note', 'music'],
        '🎶': ['musical', 'notes', 'music'],
        '🥁': ['drum', 'music', 'percussion'],
        '🪘': ['long', 'drum', 'music'],
        '🎹': ['musical', 'keyboard', 'piano'],
        '🎷': ['saxophone', 'music', 'jazz'],
        '🎺': ['trumpet', 'music', 'brass'],
        '🎸': ['guitar', 'music', 'rock'],
        '🪕': ['banjo', 'music', 'country'],
        '🎻': ['violin', 'music', 'classical'],
        '🪗': ['accordion', 'music', 'folk'],
        // Objects & Tools
        '⌚': ['watch', 'time', 'apple'],
        '📱': ['mobile', 'phone', 'smartphone'],
        '📲': ['mobile', 'phone', 'call'],
        '💻': ['laptop', 'computer', 'pc'],
        '⌨️': ['keyboard', 'computer', 'typing'],
        '🖥️': ['desktop', 'computer', 'monitor'],
        '🖨️': ['printer', 'computer', 'print'],
        '🖱️': ['computer', 'mouse', 'click'],
        '🖲️': ['trackball', 'computer', 'mouse'],
        '🕹️': ['joystick', 'gaming', 'controller'],
        '🗜️': ['clamp', 'tool', 'compress'],
        '💽': ['computer', 'disk', 'minidisc'],
        '💾': ['floppy', 'disk', 'save'],
        '💿': ['optical', 'disk', 'cd'],
        '📀': ['dvd', 'disk', 'movie'],
        '📼': ['videocassette', 'tape', 'movie'],
        '📷': ['camera', 'photo', 'picture'],
        '📸': ['camera', 'flash', 'photo'],
        '📹': ['video', 'camera', 'recording'],
        '🎥': ['movie', 'camera', 'film'],
        '📞': ['telephone', 'receiver', 'phone'],
        '☎️': ['telephone', 'phone', 'call'],
        '📟': ['pager', 'communication', 'beeper'],
        '📠': ['fax', 'machine', 'communication'],
        '📺': ['television', 'tv', 'entertainment'],
        '📻': ['radio', 'music', 'broadcast'],
        '🎙️': ['studio', 'microphone', 'recording'],
        '🎚️': ['level', 'slider', 'control'],
        '🎛️': ['control', 'knobs', 'studio'],
        '⏱️': ['stopwatch', 'timer', 'time'],
        '⏲️': ['timer', 'clock', 'countdown'],
        '⏰': ['alarm', 'clock', 'time'],
        '🕰️': ['mantelpiece', 'clock', 'time'],
        '⌛': ['hourglass', 'timer', 'sand'],
        '⏳': ['hourglass', 'flowing', 'sand'],
        '📡': ['satellite', 'antenna', 'communication'],
        '🔋': ['battery', 'power', 'energy'],
        '🪫': ['low', 'battery', 'power'],
        '🔌': ['electric', 'plug', 'power'],
        '💡': ['light', 'bulb', 'idea'],
        '🔦': ['flashlight', 'torch', 'light'],
        '🕯️': ['candle', 'light', 'flame'],
        '🪔': ['diya', 'lamp', 'oil'],
        '🧯': ['fire', 'extinguisher', 'safety'],
        '🛢️': ['oil', 'drum', 'barrel'],
        '💸': ['money', 'wings', 'flying'],
        '💴': ['yen', 'banknote', 'money'],
        '💵': ['dollar', 'banknote', 'money'],
        '💶': ['euro', 'banknote', 'money'],
        '💷': ['pound', 'banknote', 'money'],
        '🪙': ['coin', 'money', 'currency'],
        '💰': ['money', 'bag', 'dollar'],
        '💳': ['credit', 'card', 'payment'],
        '💎': ['gem', 'stone', 'diamond'],
        '⚖️': ['balance', 'scale', 'justice'],
        '🪜': ['ladder', 'climb', 'tool'],
        '🧰': ['toolbox', 'tools', 'repair'],
        '🔧': ['wrench', 'tool', 'fix'],
        '🔨': ['hammer', 'tool', 'nail'],
        '⚒️': ['hammer', 'pick', 'tool'],
        '🛠️': ['hammer', 'wrench', 'tool'],
        '⛏️': ['pick', 'tool', 'mining'],
        '🪚': ['carpentry', 'saw', 'tool'],
        '🔩': ['nut', 'bolt', 'screw'],
        '⚙️': ['gear', 'setting', 'cog'],
        '🪤': ['mouse', 'trap', 'catch'],
        '🧲': ['magnet', 'attraction', 'magnetic'],
        '🪣': ['bucket', 'pail', 'container'],
        '🧪': ['test', 'tube', 'chemistry'],
        '🧫': ['petri', 'dish', 'bacteria'],
        '🧬': ['dna', 'genetics', 'biology'],
        '🔬': ['microscope', 'science', 'lab'],
        '🔭': ['telescope', 'space', 'astronomy'],
        '💉': ['syringe', 'medicine', 'shot'],
        '🩸': ['drop', 'blood', 'donation'],
        '💊': ['pill', 'medicine', 'drug'],
        '🩹': ['bandage', 'adhesive', 'first', 'aid'],
        '🩼': ['crutch', 'walking', 'aid'],
        '🩺': ['stethoscope', 'doctor', 'medical'],
        '🚪': ['door', 'entrance', 'exit'],
        '🛗': ['elevator', 'lift', 'building'],
        '🪞': ['mirror', 'reflection', 'glass'],
        '🪟': ['window', 'glass', 'view'],
        '🛏️': ['bed', 'sleep', 'rest'],
        '🛋️': ['couch', 'lamp', 'sofa'],
        '🪑': ['chair', 'seat', 'sit'],
        '🚽': ['toilet', 'bathroom', 'wc'],
        '🪠': ['plunger', 'toilet', 'tool'],
        '🚿': ['shower', 'bath', 'water'],
        '🛁': ['bathtub', 'bath', 'water'],
        '🪒': ['razor', 'shave', 'blade'],
        '🧴': ['lotion', 'bottle', 'moisturizer'],
        '🧷': ['safety', 'pin', 'diaper'],
        '🧹': ['broom', 'cleaning', 'sweep'],
        '🧺': ['basket', 'laundry', 'picnic'],
        '🧻': ['roll', 'paper', 'toilet'],
        '🧽': ['sponge', 'cleaning', 'absorb'],
        '🛒': ['shopping', 'cart', 'trolley'],
        // Symbols & Flags
        '⚠️': ['warning', 'caution', 'alert'],
        '🚸': ['children', 'crossing', 'school'],
        '⛔': ['no', 'entry', 'prohibited'],
        '🚫': ['prohibited', 'forbidden', 'no'],
        '🚳': ['no', 'bicycles', 'prohibited'],
        '🚭': ['no', 'smoking', 'prohibited'],
        '🚯': ['no', 'littering', 'prohibited'],
        '🚱': ['non', 'potable', 'water'],
        '🚷': ['no', 'pedestrians', 'prohibited'],
        '📵': ['no', 'mobile', 'phones'],
        '🔞': ['no', 'one', 'under', 'eighteen'],
        '☢️': ['radioactive', 'nuclear', 'danger'],
        '☣️': ['biohazard', 'danger', 'toxic'],
        '⬆️': ['up', 'arrow', 'direction'],
        '↗️': ['up', 'right', 'arrow', 'direction'],
        '➡️': ['right', 'arrow', 'direction'],
        '↘️': ['down', 'right', 'arrow', 'direction'],
        '⬇️': ['down', 'arrow', 'direction'],
        '↙️': ['down', 'left', 'arrow', 'direction'],
        '⬅️': ['left', 'arrow', 'direction'],
        '↖️': ['up', 'left', 'arrow', 'direction'],
        '↕️': ['up', 'down', 'arrow', 'direction'],
        '↔️': ['left', 'right', 'arrow', 'direction'],
        '↩️': ['right', 'arrow', 'curving', 'left'],
        '↪️': ['left', 'arrow', 'curving', 'right'],
        '⤴️': ['right', 'arrow', 'curving', 'up'],
        '⤵️': ['right', 'arrow', 'curving', 'down'],
        '🔃': ['clockwise', 'vertical', 'arrows'],
        '🔄': ['counterclockwise', 'arrows', 'button'],
        '🔙': ['back', 'arrow', 'return'],
        '🔚': ['end', 'arrow', 'finish'],
        '🔛': ['on', 'arrow', 'mark'],
        '🔜': ['soon', 'arrow', 'coming'],
        '🔝': ['top', 'arrow', 'up'],
        '🛐': ['place', 'worship', 'religion'],
        '⚛️': ['atom', 'symbol', 'science'],
        '🕉️': ['om', 'hindu', 'symbol'],
        '✡️': ['star', 'david', 'jewish'],
        '☸️': ['wheel', 'dharma', 'buddhist'],
        '☯️': ['yin', 'yang', 'balance'],
        '✝️': ['latin', 'cross', 'christian'],
        '☦️': ['orthodox', 'cross', 'christian'],
        '☪️': ['star', 'crescent', 'islam'],
        '☮️': ['peace', 'symbol', 'hippie'],
        '🕎': ['menorah', 'jewish', 'hanukkah'],
        '🔯': ['dotted', 'six', 'pointed', 'star'],
        '♈': ['aries', 'zodiac', 'astrology'],
        '♉': ['taurus', 'zodiac', 'astrology'],
        '♊': ['gemini', 'zodiac', 'astrology'],
        '♋': ['cancer', 'zodiac', 'astrology'],
        '♌': ['leo', 'zodiac', 'astrology'],
        '♍': ['virgo', 'zodiac', 'astrology'],
        '♎': ['libra', 'zodiac', 'astrology'],
        '♏': ['scorpio', 'zodiac', 'astrology'],
        '♐': ['sagittarius', 'zodiac', 'astrology'],
        '♑': ['capricorn', 'zodiac', 'astrology'],
        '♒': ['aquarius', 'zodiac', 'astrology'],
        '♓': ['pisces', 'zodiac', 'astrology'],
        '⛎': ['ophiuchus', 'zodiac', 'astrology'],
        '🔀': ['twisted', 'rightwards', 'arrows'],
        '🔁': ['repeat', 'button', 'loop'],
        '🔂': ['repeat', 'single', 'button'],
        '▶️': ['play', 'button', 'start'],
        '⏩': ['fast', 'forward', 'button'],
        '⏭️': ['next', 'track', 'button'],
        '⏯️': ['play', 'pause', 'button'],
        '◀️': ['reverse', 'button', 'back'],
        '⏪': ['fast', 'reverse', 'button'],
        '⏮️': ['last', 'track', 'button'],
        '🔼': ['upwards', 'button', 'triangle'],
        '⏫': ['fast', 'up', 'button'],
        '🔽': ['downwards', 'button', 'triangle'],
        '⏬': ['fast', 'down', 'button'],
        '⏸️': ['pause', 'button', 'stop'],
        '⏹️': ['stop', 'button', 'square'],
        '⏺️': ['record', 'button', 'circle'],
        '⏏️': ['eject', 'button', 'triangle'],
        '🎦': ['cinema', 'movie', 'film'],
        '🔅': ['dim', 'button', 'brightness'],
        '🔆': ['bright', 'button', 'brightness'],
        '📶': ['antenna', 'bars', 'signal'],
        '📳': ['vibration', 'mode', 'phone'],
        '📴': ['mobile', 'phone', 'off'],
        '♀️': ['female', 'sign', 'woman'],
        '♂️': ['male', 'sign', 'man'],
        '⚧️': ['transgender', 'symbol', 'lgbtq'],
        '✖️': ['multiply', 'sign', 'x'],
        '➕': ['plus', 'sign', 'add'],
        '➖': ['minus', 'sign', 'subtract'],
        '➗': ['divide', 'sign', 'division'],
        '♾️': ['infinity', 'symbol', 'endless'],
        '‼️': ['double', 'exclamation', 'mark'],
        '⁉️': ['exclamation', 'question', 'mark'],
        '❓': ['question', 'mark', 'red'],
        '❔': ['question', 'mark', 'white'],
        '❕': ['exclamation', 'mark', 'white'],
        '❗': ['exclamation', 'mark', 'red'],
        '〰️': ['wavy', 'dash', 'squiggle'],
        '💱': ['currency', 'exchange', 'money'],
        '💲': ['heavy', 'dollar', 'sign'],
        '⚕️': ['medical', 'symbol', 'health'],
        '♻️': ['recycling', 'symbol', 'environment'],
        '⚜️': ['fleur', 'de', 'lis'],
        '🔱': ['trident', 'emblem', 'symbol'],
        '📛': ['name', 'badge', 'identification'],
        '🔰': ['japanese', 'symbol', 'beginner'],
        '⭕': ['heavy', 'large', 'circle'],
        '✅': ['check', 'mark', 'button'],
        '☑️': ['check', 'box', 'vote'],
        '✔️': ['check', 'mark', 'tick'],
        '❌': ['cross', 'mark', 'x'],
        '❎': ['cross', 'mark', 'button'],
        '➰': ['curly', 'loop', 'symbol'],
        '➿': ['double', 'curly', 'loop'],
        '〽️': ['part', 'alternation', 'mark'],
        '✳️': ['eight', 'spoked', 'asterisk'],
        '✴️': ['eight', 'pointed', 'star'],
        '❇️': ['sparkle', 'star', 'symbol'],
        '©️': ['copyright', 'symbol', 'c'],
        '®️': ['registered', 'symbol', 'r'],
        '™️': ['trade', 'mark', 'symbol'],
        // Flags (selection of common ones)
        '🏁': ['chequered', 'flag', 'racing'],
        '🚩': ['triangular', 'flag', 'red'],
        '🎌': ['crossed', 'flags', 'japan'],
        '🏴': ['black', 'flag', 'pirate'],
        '🏳️': ['white', 'flag', 'surrender'],
        '🏳️‍🌈': ['rainbow', 'flag', 'lgbtq', 'pride'],
        '🏳️‍⚧️': ['transgender', 'flag', 'lgbtq'],
        '🏴‍☠️': ['pirate', 'flag', 'skull', 'crossbones'],
        '🇺🇸': ['united', 'states', 'flag', 'america'],
        '🇬🇧': ['united', 'kingdom', 'flag', 'britain'],
        '🇯🇵': ['japan', 'flag', 'japanese'],
        '🇨🇦': ['canada', 'flag', 'canadian'],
        '🇫🇷': ['france', 'flag', 'french'],
        '🇩🇪': ['germany', 'flag', 'german'],
        '🇮🇹': ['italy', 'flag', 'italian'],
        '🇪🇸': ['spain', 'flag', 'spanish'],
        '🇷🇺': ['russia', 'flag', 'russian'],
        '🇨🇳': ['china', 'flag', 'chinese'],
        '🇰🇷': ['south', 'korea', 'flag', 'korean'],
        '🇦🇺': ['australia', 'flag', 'australian'],
        '🇧🇷': ['brazil', 'flag', 'brazilian'],
        '🇮🇳': ['india', 'flag', 'indian'],
        '🇲🇽': ['mexico', 'flag', 'mexican'],
        // Weather & Time
        '☀️': ['sun', 'sunny', 'weather'],
        '🌤️': ['sun', 'small', 'cloud', 'weather'],
        '⛅': ['sun', 'behind', 'cloud', 'weather'],
        '🌦️': ['sun', 'behind', 'rain', 'cloud'],
        '🌧️': ['cloud', 'rain', 'weather'],
        '⛈️': ['cloud', 'lightning', 'rain'],
        '🌩️': ['cloud', 'lightning', 'weather'],
        '🌨️': ['cloud', 'snow', 'weather'],
        '❄️': ['snowflake', 'snow', 'cold'],
        '☃️': ['snowman', 'snow', 'winter'],
        '⛄': ['snowman', 'without', 'snow'],
        '🌬️': ['wind', 'face', 'blowing'],
        '💧': ['droplet', 'water', 'tear'],
        '☔': ['umbrella', 'rain', 'drops'],
        '☂️': ['umbrella', 'rain', 'protection'],
        '🌊': ['water', 'wave', 'ocean'],
        '🌀': ['cyclone', 'hurricane', 'typhoon'],
        '🌈': ['rainbow', 'colorful', 'weather'],
        '🌂': ['closed', 'umbrella', 'rain'],
        '☄️': ['comet', 'space', 'tail'],
        '🔥': ['fire', 'flame', 'hot'],
        '⭐': ['star', 'night', 'sky'],
        '🌟': ['glowing', 'star', 'sparkle'],
        '✨': ['sparkles', 'magic', 'clean'],
        '⚡': ['lightning', 'bolt', 'electric'],
        '☁️': ['cloud', 'weather', 'sky'],
        '🌙': ['crescent', 'moon', 'night'],
        '🌛': ['first', 'quarter', 'moon'],
        '🌜': ['last', 'quarter', 'moon'],
        '🌚': ['new', 'moon', 'face'],
        '🌝': ['full', 'moon', 'face'],
        '🌞': ['sun', 'face', 'bright'],
        '🪐': ['saturn', 'planet', 'rings'],
        '🌠': ['shooting', 'star', 'falling']
    };
    // Check cache first for better performance
    if (emojiKeywordsCache[emoji]) {
        return emojiKeywordsCache[emoji];
    }
    // Get keywords from the main emoji keywords object
    const keywords = emojiKeywords[emoji] || [];
    // Store in cache for future use
    emojiKeywordsCache[emoji] = keywords;
    return keywords;
}
/**
 * Focuses the search input in the emoji picker
 */
function focusEmojiSearch() {
    const searchInput = document.getElementById('emoji-search');
    if (searchInput) {
        searchInput.focus();
    }
}
/**
 * Adds or removes the 'focused' class from the search input
 * @param focused Whether the input should be focused
 */
function toggleEmojiSearchFocus(focused) {
    const searchInput = document.getElementById('emoji-search');
    if (searchInput) {
        if (focused) {
            searchInput.classList.add('focused');
        }
        else {
            searchInput.classList.remove('focused');
        }
    }
}
/**
 * Filters emojis based on search query with optimized performance
 * @param query - The search query
 */
function filterEmojis(query) {
    // 検索キャッシュ
    const cache = {};
    // 入力欄の値を更新
    const searchInput = document.getElementById('emoji-search');
    if (searchInput) {
        searchInput.value = query;
    }
    const allButtons = document.querySelectorAll('.emoji-picker-button');
    const allCategories = document.querySelectorAll('.emoji-category');
    // 検索クエリが空の場合はすべて表示
    if (!query || query.trim() === '') {
        // DOM更新を最小限に抑えるために一時停止
        requestAnimationFrame(() => {
            allButtons.forEach(button => {
                button.style.display = 'inline-block';
            });
            allCategories.forEach(category => {
                category.style.display = 'block';
            });
            // 検索結果なしメッセージを削除
            const noResults = document.getElementById('emoji-no-results');
            if (noResults) {
                noResults.remove();
            }
        });
        return;
    }
    const searchTerm = query.toLowerCase().trim();
    let visibleCount = 0;
    let visibleCategories = new Set();
    // バッチ処理を最適化
    const processBatch = (buttons, startIndex, batchSize) => {
        return new Promise(resolve => {
            // 非同期で処理を行うことでUIスレッドをブロックしない
            setTimeout(() => {
                const endIndex = Math.min(startIndex + batchSize, buttons.length);
                for (let i = startIndex; i < endIndex; i++) {
                    const button = buttons[i];
                    const emoji = button.textContent?.trim() || '';
                    // キャッシュを使用して重複計算を避ける
                    if (cache[emoji] === undefined) {
                        const keywords = getEmojiKeywords(emoji);
                        cache[emoji] = keywords.some(keyword => keyword.toLowerCase().includes(searchTerm));
                    }
                    if (cache[emoji]) {
                        button.style.display = 'inline-block';
                        // カテゴリを表示
                        const category = button.closest('.emoji-category');
                        if (category) {
                            visibleCategories.add(category);
                        }
                        visibleCount++;
                    }
                    else {
                        button.style.display = 'none';
                    }
                }
                // さらに処理が必要な場合は続行
                if (endIndex < buttons.length) {
                    processBatch(buttons, endIndex, batchSize).then(resolve);
                }
                else {
                    // すべての処理が完了したら結果を表示
                    requestAnimationFrame(() => {
                        // カテゴリの表示/非表示を一括で更新
                        allCategories.forEach(category => {
                            category.style.display = visibleCategories.has(category) ? 'block' : 'none';
                        });
                        // 結果がない場合はメッセージを表示
                        if (visibleCount === 0) {
                            const noResults = document.getElementById('emoji-no-results');
                            if (!noResults) {
                                const message = document.createElement('div');
                                message.id = 'emoji-no-results';
                                message.className = 'emoji-no-results';
                                message.textContent = 'No emojis found for "' + query + '"';
                                const container = document.querySelector('.emoji-picker-content');
                                if (container) {
                                    container.appendChild(message);
                                }
                            }
                        }
                        else {
                            // 結果がある場合はメッセージを削除
                            const noResults = document.getElementById('emoji-no-results');
                            if (noResults) {
                                noResults.remove();
                            }
                        }
                        resolve();
                    });
                }
            }, 0);
        });
    };
    // 処理開始前にカテゴリを非表示にする
    requestAnimationFrame(() => {
        allCategories.forEach(category => {
            category.style.display = 'none';
        });
        // バッチサイズを調整（短いクエリの場合は大きく、長いクエリの場合は小さく）
        const batchSize = searchTerm.length <= 2 ? 30 : 50;
        processBatch(allButtons, 0, batchSize);
    });
}
// Variables for emoji picker modal
let currentEmojiResolve = null;
// イベントハンドラーを関数として定義（スコープ外からも参照できるように）
function handleCloseBtnMouseover(e) {
    const target = e.target;
    if (target) {
        target.style.backgroundColor = '#e9ecef';
    }
}
function handleCloseBtnMouseout(e) {
    const target = e.target;
    if (target) {
        target.style.backgroundColor = 'transparent';
    }
}
function handleCloseBtnClick() {
    closeEmojiPicker('');
}
function handleBackdropClick(e) {
    const backdrop = document.getElementById('emoji-picker-backdrop');
    if (e.target === backdrop) {
        closeEmojiPicker('');
    }
}
function handleEscapeKey(e) {
    if (e.key === 'Escape') {
        closeEmojiPicker('');
        document.removeEventListener('keydown', handleEscapeKey);
    }
}
/**
 * Shows the emoji picker modal and returns the selected emoji
 * @param emojiHtml - The HTML content for the emoji picker
 * @returns Promise for the selected emoji or empty string if cancelled
 */
function showEmojiPicker(emojiHtml) {
    return new Promise((resolve) => {
        currentEmojiResolve = resolve;
        // Create modal backdrop
        const backdrop = document.createElement('div');
        backdrop.id = 'emoji-picker-backdrop';
        backdrop.className = 'emoji-picker-backdrop';
        // Create modal dialog
        const modal = document.createElement('div');
        modal.id = 'emoji-picker-modal';
        modal.className = 'emoji-picker-modal';
        // Create header with close button
        const header = document.createElement('div');
        header.classList.add('emoji-modal-dynamic-header');
        const title = document.createElement('h4');
        title.textContent = 'Select Emoji';
        title.classList.add('emoji-modal-dynamic-title');
        const closeBtn = document.createElement('button');
        closeBtn.innerHTML = '&times;';
        closeBtn.classList.add('emoji-modal-dynamic-close');
        // 関数を参照するようにイベントリスナーを設定
        closeBtn.addEventListener('mouseover', handleCloseBtnMouseover);
        closeBtn.addEventListener('mouseout', handleCloseBtnMouseout);
        closeBtn.addEventListener('click', handleCloseBtnClick);
        header.appendChild(title);
        header.appendChild(closeBtn);
        // Create content area
        const content = document.createElement('div');
        content.innerHTML = emojiHtml;
        modal.appendChild(header);
        modal.appendChild(content);
        backdrop.appendChild(modal);
        // 関数を参照するように設定
        backdrop.addEventListener('click', handleBackdropClick);
        // Escape キーのイベントリスナー
        document.addEventListener('keydown', handleEscapeKey);
        document.body.appendChild(backdrop);
        // Initialize search input with a slight delay to ensure the DOM is ready
        setTimeout(() => {
            const searchInput = document.getElementById('emoji-search');
            if (searchInput) {
                searchInput.focus();
            }
        }, 100);
        // Focus the modal for keyboard navigation
        modal.tabIndex = -1;
        modal.focus();
    });
}
/**
 * Handles emoji selection from the picker
 * @param emoji - The selected emoji
 */
function selectEmoji(emoji) {
    closeEmojiPicker(emoji);
}
/**
 * Closes the emoji picker and resolves the promise
 * @param emoji - The selected emoji or empty string if cancelled
 */
function closeEmojiPicker(emoji) {
    const backdrop = document.getElementById('emoji-picker-backdrop');
    if (backdrop) {
        // イベントリスナーのクリーンアップを改善
        try {
            // クリックイベントのリスナーを削除
            backdrop.removeEventListener('click', handleBackdropClick);
            // 全てのボタンからイベントリスナーを削除
            const closeButton = backdrop.querySelector('.emoji-modal-dynamic-close');
            if (closeButton) {
                // TypeScriptの型変換を正しく処理
                const closeBtn = closeButton;
                closeBtn.removeEventListener('mouseover', handleCloseBtnMouseover);
                closeBtn.removeEventListener('mouseout', handleCloseBtnMouseout);
                closeBtn.removeEventListener('click', handleCloseBtnClick);
            }
            // Escapeキーのイベントリスナーを削除
            document.removeEventListener('keydown', handleEscapeKey);
        }
        catch (e) {
            console.error('Error cleaning up event handlers:', e);
        }
        backdrop.remove();
    }
    if (currentEmojiResolve) {
        currentEmojiResolve(emoji);
        currentEmojiResolve = null;
    }
}
// Assign functions to window object for global access
window.getEmojiKeywords = getEmojiKeywords;
window.focusEmojiSearch = focusEmojiSearch;
window.toggleEmojiSearchFocus = toggleEmojiSearchFocus;
window.filterEmojis = filterEmojis;
window.showEmojiPicker = showEmojiPicker;
window.selectEmoji = selectEmoji;
export {};
//# sourceMappingURL=EmojiPicker.razor.js.map