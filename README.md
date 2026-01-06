# JLPT Reference

JLPT Reference is an open source full Japanese dictionary designed to help students master vocabulary and kanji. It provides a modern interface with powerful search capabilities, detailed stroke animations, and comprehensive dictionary data.

## Features

### Search and Lookup
*   **Custom Search Engines**: Intelligent search with backend ranking mechanisms to provide the most relevant results first.
*   **Full Tag Search**: Filter results using a comprehensive tagging system, similar to GitHub issue search.
*   **Flexible Input**: Support for searching with kana, romaji, kanji, and hiragana.
*   **Automatic Transliteration**: Backend processing automatically handles transliterations, such as converting romaji to katakana or hiragana.
*   **Radical Lookup**: Find kanji by identifying their component radicals, replicating the experience of a traditional paper dictionary.

### Visual and Audio
*   **Pronunciation**: Audio pronunciation for vocabulary using Google Voice.
*   **Stroke Animations**: Full stroke order animations for all available vocabulary and kanji, powered by KanjiVG data.

## Visuals

![Search Interface](file:///c:/Projects/JLPTReference/demo/search.jpg)
![Kanji Details](file:///c:/Projects/JLPTReference/demo/kanji_details.jpg)
![Custom Tags](file:///c:/Projects/JLPTReference/demo/custom_tags.jpg)
![Dark Mode](file:///c:/Projects/JLPTReference/demo/about_page_dark_mode.jpg)

## Technology Stack

The project is built using modern web and backend technologies:

*   **Frontend**: Vue 3, TypeScript, Vuetify 3, Pinia
*   **Backend**: .NET 8
*   **Database**: PostgreSQL
*   **Containerization**: Docker

## Libraries and Projects

We leverage several excellent libraries and projects:

*   **vue-dmak**: Used for kanji stroke animations. [Link](https://github.com/karollan/vue-dmak)
*   **jmdict-simplified**: EDRDG XML to JSON parser. [Link](https://github.com/scriptin/jmdict-simplified) | License: [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/)
*   **WanaKana-net**: Library for transliteration of Hiragana, Katakana, and Romaji. [Link](https://github.com/MartinZikmund/WanaKana-net/tree/dev)

## Attributions and Resources

This project would not be possible without the incredible data provided by the Japanese language learning community.

### Dictionary Data

*   **JMdict**: Comprehensive Japanese-Multilingual Dictionary file by the Electronic Dictionary Research and Development Group (EDRDG). [Link](https://www.edrdg.org/jmdict/j_jmdict.html) | License: [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/)
*   **KANJIDIC2**: Detailed information on over 13,000 kanji characters by the Electronic Dictionary Research and Development Group (EDRDG). [Link](https://www.edrdg.org/wiki/index.php/KANJIDIC_Project) | License: [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/)
*   **RADKFILE/KRADFILE**: Radical data for kanji characters by the Electronic Dictionary Research and Development Group (EDRDG). [Link](https://www.edrdg.org/krad/kradinf.html) | License: [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/)
*   **KanjiVG**: Stroke order data for kanji characters. [Link](https://github.com/KanjiVG/kanjivg) | License: [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/)
*   **JmdictFurigana**: Furigana data for kanji characters. [Link](https://github.com/Doublevil/JmdictFurigana) | License: [MIT](http://github.com/Doublevil/JmdictFurigana?tab=MIT-1-ov-file)
*   **kanji-data**: Additional kanji data for kanji characters. [Link](https://github.com/davidluzgouveia/kanji-data/tree/master) | License: [MIT](https://github.com/davidluzgouveia/kanji-data/tree/master?tab=MIT-1-ov-file)
*   **kanjium**: Enhancement for radical data. [Link](https://github.com/mifunetoshiro/kanjium/tree/master) | License: [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/)
*   **JLPT vocabulary by level**: JLPT level for vocabulary data by Robin Pourtaud. [Link](https://www.kaggle.com/datasets/robinpourtaud/jlpt-words-by-level) | License: [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/)

## Getting Started

To run the project locally using Docker, please refer to the [Docker Setup Guide](file:///c:/Projects/JLPTReference/DOCKER_README.md).
