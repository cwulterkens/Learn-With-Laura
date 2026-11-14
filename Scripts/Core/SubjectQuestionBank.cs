using System;
using System.Collections.Generic;
using System.Linq;

public static class SubjectQuestionBank
{
    public const int QuestionsPerGrade = 64;
    public const int Grades = 8;
    public const int QuestionsPerSubject = QuestionsPerGrade * Grades;

    private static readonly string[] Names = { "Ada", "Ben", "Clara", "Diego", "Eli", "Fern", "Grace", "Hugo" };
    private static readonly string[] Places = { "Pine Creek", "Oak Hollow", "Prairie Bend", "Maple Crossing", "Cedar Hill", "River Fork", "Willow Point", "Stone Ridge" };

    public static bool Supports(string subjectKey)
    {
        return subjectKey is "science" or "history" or "social_studies" or "geography" or "english";
    }

    public static IReadOnlyList<ChoiceQuestion> GetQuestions(string subjectKey, int grade)
    {
        int safeGrade = Math.Clamp(grade, 1, 8);
        var questions = new List<ChoiceQuestion>(QuestionsPerGrade);
        for (int index = 0; index < QuestionsPerGrade; index++)
        {
            questions.Add(subjectKey switch
            {
                "science" => BuildScience(safeGrade, index),
                "history" => BuildHistory(safeGrade, index),
                "social_studies" => BuildSocialStudies(safeGrade, index),
                "geography" => BuildGeography(safeGrade, index),
                "english" => BuildEnglish(safeGrade, index),
                _ => throw new ArgumentOutOfRangeException(nameof(subjectKey), subjectKey, "Unknown subject")
            });
        }
        return questions;
    }

    public static IReadOnlyList<ChoiceQuestion> GetSubjectCatalog(string subjectKey)
    {
        return Enumerable.Range(1, Grades)
            .SelectMany(grade => GetQuestions(subjectKey, grade))
            .ToList();
    }

    private static ChoiceQuestion BuildScience(int grade, int index)
    {
        int variant = index / 8;
        int a = 4 + grade + variant;
        int b = a + 3 + variant;
        string name = Names[(grade + variant) % Names.Length];
        string place = Places[(index + grade) % Places.Length];
        return (index % 8) switch
        {
            0 => Make("science", grade, index, "interpreting_data",
                $"At {place}, cup A is {a}°C and cup B is {b}°C. Which cup has the higher measured temperature?",
                "Cup B", "Cup A", "They are equal", "Temperature cannot be compared",
                $"{b} is greater than {a}, so cup B has the higher measured temperature.",
                "Compare measurements only when their units match, then identify the greater value.",
                $"On a number line, {b} is {b - a} units to the right of {a}.", "Next Generation Science Standards Topic Arrangements", "https://www.nextgenscience.org/overview-topics"),
            1 => Make("science", grade, index, "fair_tests",
                $"{name} tests whether light affects bean growth. Pot A gets {a} hours of light and pot B gets {b} hours. What should stay the same for a fair test?",
                "The bean type, soil, water, and pot size", "The hours of light", "Every condition should change", "The result should be chosen first",
                "A fair test changes the light while keeping other important conditions alike.",
                "Name the one variable being tested. Keep the comparison conditions the same and measure the result in the same way.",
                "Using equal pots and equal water makes light the main planned difference.", "Next Generation Science Standards Topic Arrangements", "https://www.nextgenscience.org/overview-topics"),
            2 => Make("science", grade, index, "sound_and_vibration",
                $"Investigation {grade}-{variant + 1}: {name} plucks a stretched string and hears a sound. What is the string doing?",
                "Vibrating", "Losing all its mass", "Changing into a liquid", "Producing its own light",
                "A plucked string vibrates and makes the surrounding air vibrate, carrying sound.",
                "Look for repeated back-and-forth motion in the object producing the sound.",
                "A drumhead and a tuning fork also vibrate when they produce sound.", "Next Generation Science Standards Topic Arrangements", "https://www.nextgenscience.org/overview-topics"),
            3 => Make("science", grade, index, "ecosystem_resources",
                $"Habitat report {grade}-{variant + 1}: A rabbit population near {place} has shelter and water but very little food. Which missing resource most directly limits the habitat?",
                "Food", "Moonlight", "A road sign", "A thermometer",
                "Animals need usable food, water, shelter, and space. Having some resources does not replace a missing essential resource.",
                "List the organism's basic needs, then compare that list with the resources named in the scenario.",
                "A pond may supply water, but it cannot by itself replace food or shelter.", "Next Generation Science Standards Topic Arrangements", "https://www.nextgenscience.org/overview-topics"),
            4 => Make("science", grade, index, "matter_changes",
                $"Sample {grade * 10 + variant + 1} is ice in a sealed container. After it melts, what has changed?",
                "Its state changed from solid to liquid", "All matter disappeared", "It became a different element", "Its mass had to double",
                "Melting is a change of state. In a sealed system, the matter remains even though its form changes.",
                "Separate a change in state, shape, or size from evidence that a new substance formed.",
                "Liquid water can freeze back into solid ice, showing that the state change is reversible.", "Next Generation Science Standards Topic Arrangements", "https://www.nextgenscience.org/overview-topics"),
            5 => Make("science", grade, index, "forces_and_motion",
                $"Cart trial {grade}-{variant + 1}: Two equal toy carts roll on the same floor. Cart A is pushed gently and cart B is pushed more strongly. Which result is most reasonable to test?",
                "Cart B changes speed more", "Cart B loses its color", "Cart A stops having mass", "Both must move exactly the same",
                "With other conditions held alike, a stronger net force can produce a greater change in motion.",
                "Keep the carts and surface the same, change the push, and measure distance or speed over the same interval.",
                "A harder push on the same cart usually changes its motion more than a gentle push.", "Next Generation Science Standards Topic Arrangements", "https://www.nextgenscience.org/overview-topics"),
            6 => Make("science", grade, index, "food_webs",
                $"In food-web model {grade}-{variant + 1}, grass is eaten by a rabbit, and the rabbit is eaten by a hawk. Where did the grass's stored energy begin?",
                "Sunlight", "The hawk", "Soil alone", "The rabbit's fur",
                "Plants capture light energy to make sugars. That stored energy can move through a food web.",
                "Trace energy arrows backward from consumers to producers, then identify the producer's energy source.",
                "Sun → grass → rabbit → hawk shows the direction energy moves in this simple model.", "Next Generation Science Standards Topic Arrangements", "https://www.nextgenscience.org/overview-topics"),
            _ => Make("science", grade, index, "evidence_and_claims",
                $"{name} claims that a wool cover slows cooling. Covered cup A cools {a}° and uncovered cup B cools {b}°. Which evidence supports the claim?",
                $"Cup A cooled {b - a}° less", "Both cups were labeled", "Cup B had a letter in its name", "No temperatures were measured",
                $"The covered cup's temperature changed less: {b} - {a} = {b - a} degrees less cooling.",
                "State the claim, identify the measurement that would differ if it were true, and compare the measured changes.",
                "Less cooling means more thermal energy remained in the covered cup during the test.", "Next Generation Science Standards Topic Arrangements", "https://www.nextgenscience.org/overview-topics")
        };
    }

    private static ChoiceQuestion BuildHistory(int grade, int index)
    {
        int variant = index / 8;
        int firstYear = 1800 + grade * 10 + variant * 3;
        int middleYear = firstYear + 7;
        int lastYear = middleYear + 11;
        string name = Names[(grade + variant) % Names.Length];
        string place = Places[(index + grade) % Places.Length];
        return (index % 8) switch
        {
            0 => Make("history", grade, index, "chronology",
                $"A timeline for {place} lists {firstYear}, {middleYear}, and {lastYear}. Which year comes last?",
                lastYear.ToString(), firstYear.ToString(), middleYear.ToString(), (firstYear - 1).ToString(),
                $"{lastYear} is the greatest year and therefore the latest date on this timeline.",
                "Order years from least to greatest. Later years have greater numbers when all dates use the same era.",
                $"{firstYear} → {middleYear} → {lastYear} is chronological order.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            1 => Make("history", grade, index, "primary_sources",
                $"To study daily life in {place} in {middleYear}, which item would be a primary source?",
                $"A diary written there in {middleYear}", "A modern novel set there", "An undated guess", "A summary written without evidence",
                "A diary created in the time and place being studied is direct evidence, though its perspective still has limits.",
                "Ask when the source was created and how directly its creator experienced the event or period.",
                "A later textbook can be useful, but it is secondary for studying the earlier period.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            2 => Make("history", grade, index, "corroboration",
                $"{name}'s letter says a storm damaged every farm near {place}. What is the best next step for a historian?",
                "Compare the letter with other letters, newspapers, and records", "Assume every word describes every farm", "Ignore who wrote it", "Change the date to fit the claim",
                "Corroborating independent sources helps test how broadly one person's statement applies.",
                "Identify the claim, then seek sources with different creators or evidence that could confirm, narrow, or challenge it.",
                "A newspaper, weather record, and several farm diaries could reveal where damage actually occurred.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            3 => Make("history", grade, index, "cause_and_effect",
                $"Records show a bridge opened near {place} in {firstYear}, and travel time across the river then fell. Which statement is a supported effect?",
                "Crossing the river became faster", "The bridge changed the year", "Every traveler moved away", "The river stopped flowing",
                "The evidence directly connects the bridge opening with reduced crossing time.",
                "Separate the event from what followed, and choose an effect that the evidence actually measures.",
                "A new route may reduce travel time without proving that every other part of life changed.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            4 => Make("history", grade, index, "continuity_and_change",
                $"Maps from {firstYear} and {lastYear} both show farms near {place}, but the later map adds a railway. What stayed continuous?",
                "Farming remained present", "The railway was on both maps", "All transportation disappeared", "The dates were identical",
                "The farms appear in both sources, while the railway is a change shown only on the later map.",
                "Make two lists: features present at both times and features that appeared, disappeared, or changed.",
                "A feature can continue even while other technology and settlement patterns change.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            5 => Make("history", grade, index, "source_purpose",
                $"A merchant's advertisement from {middleYear} praises every product in the shop. Why should a historian consider its purpose?",
                "It was designed to persuade customers", "Advertisements contain no historical evidence", "Its date cannot matter", "Persuasion makes every claim false",
                "The advertisement can show products and sales language, but its persuasive purpose may shape its claims.",
                "Ask who created the source, for which audience, and what response the creator wanted.",
                "A sales notice may reliably list an item while exaggerating its quality.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            6 => Make("history", grade, index, "claims_and_evidence",
                $"Which evidence best supports the claim that school attendance at {place} increased between {firstYear} and {lastYear}?",
                "Comparable attendance registers from both years", "One student's favorite lesson", "A map without a school", "A story with no date",
                "Comparable records from both dates can show whether the number attending changed.",
                "Match the evidence to every part of the claim: topic, place, dates, and type of change.",
                "If registers list 18 students first and 31 later, that is direct evidence of an increase.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            _ => Make("history", grade, index, "historical_context",
                $"A {lastYear} account criticizes travel in {firstYear}. What context should be checked before comparing the two periods?",
                "Available transportation and road conditions in each period", "Only the writer's handwriting size", "Whether both years rhyme", "A forecast for next week",
                "Travel choices and expectations depend on the technology, roads, laws, and conditions of their time.",
                "Avoid judging the past only by present expectations. Identify the conditions and choices actually available then.",
                "A trip before a railway opened should not be evaluated as if the railway already existed.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf")
        };
    }

    private static ChoiceQuestion BuildSocialStudies(int grade, int index)
    {
        int variant = index / 8;
        int cost = 3 + grade + variant;
        string name = Names[(grade + variant) % Names.Length];
        string place = Places[(index + grade) % Places.Length];
        return (index % 8) switch
        {
            0 => Make("social_studies", grade, index, "needs_and_wants",
                $"{name} has enough money for drinking water or a decorative ribbon at the {place} market. Which is a basic need?",
                "Drinking water", "The decorative ribbon", "Both are required for survival", "Neither has any value",
                "Water is a basic need. A ribbon may be wanted and valued, but it is not required for survival.",
                "Ask whether a person needs the item for health and safety or would simply enjoy having it.",
                "Food and shelter are needs; a second toy is usually a want.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            1 => Make("social_studies", grade, index, "goods_and_services",
                $"At {place}, a carpenter repairs {name}'s chair for {cost} coins. What did the carpenter provide?",
                "A service", "A natural resource", "A weather event", "A voting district",
                "Repair work is a service—useful work performed for someone. The repaired chair is a good.",
                "A good is a physical item; a service is work someone performs.",
                "Bread is a good, while delivering the bread is a service.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            2 => Make("social_studies", grade, index, "rules_and_laws",
                $"Safety proposal {grade}-{variant + 1}: Why might {place} adopt a rule requiring wagons to slow near a busy crossing?",
                "To reduce the risk of collisions", "To make distance longer", "To remove every choice", "To change the weather",
                "A clear traffic rule can coordinate behavior and protect people using a shared crossing.",
                "Identify the shared problem, then ask whether the rule addresses it fairly and can be followed.",
                "A school rule to walk near a hot stove addresses a specific safety risk.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            3 => Make("social_studies", grade, index, "civic_decision_making",
                $"Public proposal {grade}-{variant + 1}: Residents of {place} disagree about where to build a public well. Which process best supports a fair decision?",
                "Share evidence, hear affected residents, and use an agreed process", "Let one rumor decide", "Hide the costs", "Exclude everyone who disagrees",
                "Public decisions are stronger when people can examine evidence, express concerns, and follow transparent rules.",
                "Name who is affected, gather relevant evidence, compare tradeoffs, and use the community's lawful decision process.",
                "A meeting can compare distance, water quality, cost, and accessibility before a vote.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            4 => Make("social_studies", grade, index, "public_goods_and_taxes",
                $"Project list {grade}-{variant + 1}: Which project is the clearest example of a shared public service for {place}?",
                "Maintaining the public road", "Buying one person's private collection", "Choosing one family's supper", "Painting a locked private room",
                "A public road serves many community members and is commonly maintained through public decisions and revenue.",
                "Ask whether many people can use the service and whether government has responsibility for providing or maintaining it.",
                "Libraries, fire protection, and public roads are common shared services.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            5 => Make("social_studies", grade, index, "levels_of_government",
                $"Government case {grade}-{variant + 1}: Which responsibility is most likely handled by the local government of {place}?",
                "Maintaining a town street", "Negotiating a treaty with another country", "Printing national currency", "Admitting a new state",
                "Local governments commonly maintain local streets, while national responsibilities include treaties and currency.",
                "Identify the geographic reach of the issue, then compare it with local, state, and national powers.",
                "A town council may address a local sidewalk; Congress makes national laws.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            6 => Make("social_studies", grade, index, "scarcity_and_tradeoffs",
                $"{place} has {cost} workdays available but two projects that each require all {cost} days. What is the opportunity cost of choosing the bridge repair?",
                "The other project that cannot be completed", "The days become unlimited", "Both projects finish automatically", "There is no tradeoff",
                "Scarcity requires a choice. The opportunity cost is the next-best alternative given up.",
                "List the realistic alternatives, identify the chosen one, and name the best alternative that must be forgone.",
                "If time is spent repairing a bridge, that same limited time cannot also repair the roof.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            _ => Make("social_studies", grade, index, "rights_and_responsibilities",
                $"{name} wants to speak at a {place} meeting. Which action pairs a civic right with a responsibility?",
                "Share a view while following the meeting's fair rules", "Silence every other speaker", "Invent evidence", "Prevent lawful participation",
                "Participation includes the right to express a view and the responsibility to respect fair procedures and others' rights.",
                "Identify the right involved, then consider what conduct lets everyone exercise their rights safely and fairly.",
                "Listening during another person's turn supports the same participation right you expect for yourself.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf")
        };
    }

    private static ChoiceQuestion BuildGeography(int grade, int index)
    {
        int variant = index / 8;
        int scale = 2 + grade + variant;
        int mapDistance = 2 + variant;
        string name = Names[(grade + variant) % Names.Length];
        string place = Places[(index + grade) % Places.Length];
        return (index % 8) switch
        {
            0 => Make("geography", grade, index, "cardinal_directions",
                $"On map {grade}-{variant + 1}, north is at the top. {place} is directly right of {name}'s farm. Which direction is the town from the farm?",
                "East", "West", "North", "South",
                "When north is at the top, east is to the right.",
                "Mark north first, then use the compass directions clockwise: north, east, south, west.",
                "On a north-up map, left is west and down is south.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            1 => Make("geography", grade, index, "map_scale",
                $"An imagined map of {place} uses 1 centimeter for {scale} miles. Two sites are {mapDistance} centimeters apart. What distance does the map represent?",
                $"{scale * mapDistance} miles", $"{scale + mapDistance} miles", $"{scale} miles", $"{mapDistance} miles",
                $"Multiply map distance by the scale: {mapDistance} × {scale} = {scale * mapDistance} miles.",
                "Write the scale as miles per centimeter, then multiply by the measured map centimeters.",
                $"{mapDistance} cm × {scale} miles per cm = {scale * mapDistance} miles.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            2 => Make("geography", grade, index, "grid_references",
                $"On the {place} grid, columns are A–H and rows are 1–8. The school is in column {(char)('A' + variant)}, row {grade}. What is its grid reference?",
                $"{(char)('A' + variant)}{grade}", $"{grade}{(char)('A' + variant)}", $"{(char)('A' + variant)}-{grade}", "No grid reference",
                "A grid reference names the column first and the row second.",
                "Trace the column and row until they intersect, then write the column label followed by the row label.",
                "Column C and row 4 intersect at C4.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            3 => Make("geography", grade, index, "population_patterns",
                $"Area A near {place} has {10 + variant} homes in one square mile. Area B has {18 + variant} homes in one square mile. Which is more densely settled?",
                "Area B", "Area A", "They are equal", "Density cannot use area",
                $"The areas are the same size, and {18 + variant} is greater than {10 + variant}, so area B has more homes per square mile.",
                "Compare the number of people or homes within equal areas. For unequal areas, divide by area first.",
                "Twenty people in one square mile is denser than ten people in the same area.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf"),
            4 => Make("geography", grade, index, "weather_and_climate",
                $"Climate file {grade}-{variant + 1}: Which evidence best describes the usual winter climate of {place}?",
                "Temperature and precipitation records collected over many years", "One afternoon's weather", "One undated photograph", "A traveler's favorite color",
                "Climate describes long-term patterns, so comparable records across many years provide stronger evidence than one event.",
                "Check the time span and whether the same measurements were collected consistently.",
                "A 30-year monthly record reveals typical conditions and variation better than a single forecast.", "Next Generation Science Standards Topic Arrangements", "https://www.nextgenscience.org/overview-topics"),
            5 => Make("geography", grade, index, "watersheds",
                $"Watershed report {grade}-{variant + 1}: Rain falls upstream from {place} and carries loose soil into a creek. Why can that matter downstream?",
                "Connected water can transport sediment", "Water cannot move downhill", "Watersheds follow only town borders", "Upstream and downstream never connect",
                "Runoff and streams move water and materials through connected drainage networks.",
                "Trace the direction water flows and identify what it can carry from one part of the watershed to another.",
                "Soil eroded upstream may make downstream water muddier.", "USGS Watersheds and Drainage Basins", "https://www.usgs.gov/water-science-school/science/watersheds-and-drainage-basins"),
            6 => Make("geography", grade, index, "map_layers",
                $"Route plan {grade}-{variant + 1}: A planner in {place} needs a route near roads but outside flood-prone land. Which map information should be compared?",
                "Road and flood-risk layers", "A decorative border only", "A list with no locations", "Unrelated town names",
                "Comparing aligned road and flood-risk information identifies places meeting both spatial conditions.",
                "Translate each requirement into a map layer, align the layers, and find locations where all conditions are satisfied.",
                "A suitable site can be close to a road while remaining outside the mapped flood zone.", "USGS Topographic Maps", "https://www.usgs.gov/programs/national-geospatial-program/topographic-maps"),
            _ => Make("geography", grade, index, "regions",
                $"Map pair {grade}-{variant + 1}: Two maps draw the boundary of the {place} region differently. How can both maps still be useful?",
                "They may use different stated criteria", "Every region has one required border", "One map must hide all evidence", "Regions can never be mapped",
                "Regions are defined for particular purposes using criteria such as landforms, climate, vegetation, economics, or culture.",
                "Identify each map's criteria and check whether each boundary applies its own criteria consistently.",
                "A climate region and a cultural region may overlap without sharing the same boundary.", "C3 Framework for Social Studies", "https://www.socialstudies.org/sites/default/files/c3/c3-framework-for-social-studies-rev0617.pdf")
        };
    }

    private static ChoiceQuestion BuildEnglish(int grade, int index)
    {
        int variant = index / 8;
        string name = Names[(grade + variant) % Names.Length];
        string place = Places[(index + grade) % Places.Length];
        string objectName = new[] { "lantern", "basket", "notebook", "blanket", "apple", "wagon", "pencil", "window" }[variant];
        return (index % 8) switch
        {
            0 => Make("english", grade, index, "capitalization",
                $"Which sentence uses capitalization correctly in writing sample {grade}-{variant + 1}?",
                $"{name} walked to {place}.", $"{name.ToLower()} walked to {place}.", $"{name} walked to {place.ToLower()}.", $"{name.ToLower()} walked to {place.ToLower()}.",
                "A sentence begins with a capital letter, and the names of a person and a specific place are capitalized.",
                "Check the first word and every proper noun. Ordinary common nouns stay lowercase unless they begin the sentence.",
                "In “Maya crossed Pine Creek,” Maya and Pine Creek are proper nouns.", "Common Core State Standards for English Language Arts", "https://www.thecorestandards.org/ELA-Literacy/"),
            1 => Make("english", grade, index, "punctuation",
                $"Which punctuation best completes this sentence: “Where did {name} put the {objectName}___”",
                "?", ".", "!", ",",
                "The sentence asks a direct question, so it ends with a question mark.",
                "Read the sentence for purpose: a question asks, a statement tells, and an exclamation expresses strong feeling.",
                "“When will the train arrive?” ends with a question mark.", "Common Core State Standards for English Language Arts", "https://www.thecorestandards.org/ELA-Literacy/"),
            2 => Make("english", grade, index, "parts_of_speech_nouns",
                $"In the sentence “{name} carried the {objectName} carefully,” which word is a noun naming a thing?",
                objectName, "carried", "carefully", name,
                $"“{objectName}” names a thing. “Carried” is an action verb, and “carefully” is an adverb.",
                "Find the word that names a person, place, thing, or idea. Then check its job in the sentence.",
                "In “The dog barked loudly,” dog is a noun, barked is a verb, and loudly is an adverb.", "Common Core State Standards for English Language Arts", "https://www.thecorestandards.org/ELA-Literacy/"),
            3 => Make("english", grade, index, "parts_of_speech_verbs",
                $"Which word is the action verb in “At {place}, {name} opened the {objectName}”?",
                "opened", name, place, objectName,
                "“Opened” tells what the subject did, so it is the action verb.",
                "Identify who or what the sentence is about, then ask what that subject did or is doing.",
                "In “The horse pulled the cart,” pulled is the action verb.", "Common Core State Standards for English Language Arts", "https://www.thecorestandards.org/ELA-Literacy/"),
            4 => Make("english", grade, index, "word_forms",
                $"Word list {grade}-{variant + 1}: Which phrase correctly shows more than one {objectName}?",
                $"two {RegularPlural(objectName)}", $"two {objectName}'s", $"two {objectName}es's", $"two very {objectName}",
                $"The regular plural of “{objectName}” is “{RegularPlural(objectName)}.” An apostrophe does not make a regular noun plural.",
                "For most regular nouns, add s. Use es after certain endings, and learn irregular plurals separately.",
                "Book becomes books; box becomes boxes; child becomes children.", "Common Core State Standards for English Language Arts", "https://www.thecorestandards.org/ELA-Literacy/"),
            5 => Make("english", grade, index, "sequence_and_structure",
                $"Passage {grade}-{variant + 1}: “First, {name} filled the pail. Next, {name} watered the garden. Finally, the pail was returned.” What happened second?",
                "The garden was watered", "The pail was filled", "The pail was returned", "The garden was planted next year",
                "The signal word “Next” introduces the second event: watering the garden.",
                "Underline sequence words such as first, next, then, and finally. Match each word with its event.",
                "In “First mix, then bake,” baking happens second.", "Common Core State Standards for English Language Arts", "https://www.thecorestandards.org/ELA-Literacy/"),
            6 => Make("english", grade, index, "context_clues",
                $"In “After the long walk to {place}, {name} was weary and rested,” what does “weary” most likely mean?",
                "Tired", "Noisy", "Invisible", "Unfinished",
                "The long walk and the decision to rest are context clues showing that weary means tired.",
                "Read the words around the unfamiliar word and look for examples, contrasts, causes, or results that reveal meaning.",
                "In “The glass was fragile, so she carried it gently,” gently helps show that fragile means easily broken.", "Common Core State Standards for English Language Arts", "https://www.thecorestandards.org/ELA-Literacy/"),
            _ => Make("english", grade, index, "text_evidence",
                $"Passage {grade}-{variant + 1}: “Dark clouds gathered over {place}. {name} brought the {objectName} inside before the rain began.” Which detail best shows preparation?",
                $"{name} brought the {objectName} inside", "Clouds were in the sky", "The place had a name", "Rain is made of water",
                "Bringing the item inside before the rain is a specific action taken to prepare.",
                "State the idea you need to prove, then choose the passage detail that most directly supports it.",
                "For the claim “Lee prepared for cold,” the detail “Lee packed a coat” is stronger than “Lee looked outside.”", "Common Core State Standards for English Language Arts", "https://www.thecorestandards.org/ELA-Literacy/")
        };
    }

    private static ChoiceQuestion Make(
        string subject,
        int grade,
        int index,
        string skill,
        string prompt,
        string correct,
        string distractorA,
        string distractorB,
        string distractorC,
        string why,
        string howTo,
        string example,
        string sourceTitle,
        string sourceUrl)
    {
        string[] rawOptions = { correct, distractorA, distractorB, distractorC };
        int correctIndex = (grade + index) % 4;
        var options = new List<string>(4);
        for (int optionIndex = 0; optionIndex < 4; optionIndex++)
        {
            int sourceIndex = (optionIndex - correctIndex + 4) % 4;
            options.Add(rawOptions[sourceIndex]);
        }

        return new ChoiceQuestion
        {
            Id = $"generated_{subject}_g{grade}_{index:00}",
            GradeMin = grade,
            GradeMax = grade,
            SkillId = skill,
            Canon = "general",
            Prompt = prompt,
            Options = options,
            CorrectIndex = correctIndex,
            Hint = howTo,
            SuccessText = why,
            HowTo = howTo,
            Example = example,
            SourceTitle = sourceTitle,
            SourceUrl = sourceUrl
        };
    }

    private static string RegularPlural(string word)
    {
        return word.EndsWith("s", StringComparison.OrdinalIgnoreCase)
            || word.EndsWith("x", StringComparison.OrdinalIgnoreCase)
            || word.EndsWith("ch", StringComparison.OrdinalIgnoreCase)
            || word.EndsWith("sh", StringComparison.OrdinalIgnoreCase)
            ? word + "es"
            : word + "s";
    }
}
